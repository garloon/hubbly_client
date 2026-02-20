using Hubbly.Mobile.Models;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Hubbly.Mobile.Services;

public class SignalRService : IDisposable
{
    private readonly TokenManager _tokenManager;
    private readonly AuthService _authService;
    private readonly ILogger<SignalRService> _logger;

    // Синхронизация
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);

    // Соединение
    private HubConnection _hubConnection;
    private readonly List<IDisposable> _subscriptions = new();
    private readonly CancellationTokenSource _cts = new();
    
    private Func<Exception?, Task> _connectionClosedHandler;

    // Состояние
    private bool _isConnecting = false;
    private bool _isConnected = false;
    private DateTime _lastConnectionAttempt = DateTime.MinValue;

    // Реконнект
    private int _reconnectAttempts = 0;
    private const int MaxReconnectAttempts = 5;
    private const int ReconnectBaseDelayMs = 2000;

    // Heartbeat
    private Timer _heartbeatTimer;
    private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _heartbeatTimeout = TimeSpan.FromSeconds(10);
    private string _currentUserId;

    // Очередь сообщений для offline режима
    private readonly ConcurrentQueue<QueuedMessage> _messageQueue = new();
    private readonly Timer _queueProcessorTimer;

    // События
    public event EventHandler<ChatMessage> OnMessageReceived;
    public event EventHandler<IEnumerable<UserJoinedData>> OnInitialPresenceData;
    public event EventHandler<IEnumerable<UserJoinedData>> OnInitialPresenceReceived;
    public event EventHandler<RoomAssignmentData> OnAssignedToRoom;
    public event EventHandler<UserJoinedData> OnUserJoined;
    public event EventHandler<UserLeftData> OnUserLeft;
    public event EventHandler<string> OnUserTyping;
    public event EventHandler<string> OnErrorReceived;
    public event EventHandler OnAuthenticationFailed;
    public event EventHandler<ConnectionStateChangedEventArgs> OnConnectionStateChanged;
    public event EventHandler<string> OnDebugMessage;

    // Свойства
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected && _isConnected;
    public HubConnectionState ConnectionState => _hubConnection?.State ?? HubConnectionState.Disconnected;

    public SignalRService(TokenManager tokenManager, AuthService authService, ILogger<SignalRService> logger)
    {
        _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _logger = logger;

        // Читаем server URL из Preferences
        var serverUrl = Preferences.Get("server_url", "http://89.169.46.33:5000");
        _hubUrl = $"{serverUrl.TrimEnd('/')}/chatHub";
        
        _logger.LogInformation("🚀 SignalRService created with URL: {Url}", _hubUrl);

        // Таймер для обработки очереди сообщений
        _queueProcessorTimer = new Timer(ProcessMessageQueue, null,
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

        // Получаем текущего пользователя
        Task.Run(async () =>
        {
            _currentUserId = await _tokenManager.GetAsync("user_id");
        });
    }

    #region Публичные методы

    public async Task StartConnection()
    {
        _logger.LogInformation("📞 StartConnection called. Current state: {State}", _hubConnection?.State);
        // Защита от слишком частых попыток
        if ((DateTime.Now - _lastConnectionAttempt).TotalSeconds < 2)
        {
            _logger.LogWarning("SignalR: Too frequent connection attempts, throttling");
            return;
        }

        if (!await _connectionLock.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            _logger.LogError("SignalR: Failed to acquire lock within 5 seconds");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            {
                State = ConnectionState,
                Error = "Lock timeout"
            });
            return;
        }

        try
        {
            if (_cts.IsCancellationRequested)
            {
                _logger.LogWarning("SignalR: Service is disposed, cannot start");
                return;
            }

            if (_isConnecting)
            {
                _logger.LogInformation("SignalR: Already connecting, skipping");
                return;
            }

            if (_hubConnection?.State == HubConnectionState.Connected || _isConnected)
            {
                _logger.LogInformation($"SignalR: Already connected. State: {_hubConnection?.State}");
                OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
                {
                    State = ConnectionState
                });
                return;
            }

            if (_hubConnection?.State == HubConnectionState.Connecting)
            {
                _logger.LogInformation("SignalR: Connection in progress");
                return;
            }

            _isConnecting = true;
            _lastConnectionAttempt = DateTime.Now;
            _reconnectAttempts = 0;

            _logger.LogInformation("SignalR: Starting new connection...");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            {
                State = HubConnectionState.Connecting
            });

            // Dispose старого соединения если есть
            await DisposeOldConnectionAsync();

            // Создаем новое соединение
            _hubConnection = await CreateConnectionAsync();

            // Запускаем
            await _hubConnection.StartAsync(_cts.Token);

            _isConnected = true;
            _reconnectAttempts = 0;

            // Запускаем heartbeat
            StartHeartbeat();

            _logger.LogInformation($"SignalR: Connected successfully. State: {_hubConnection.State}");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            {
                State = _hubConnection.State
            });

            // Отправляем накопленные сообщения
            _ = Task.Run(() => ProcessMessageQueue(null));
        }
        catch (HttpRequestException ex) when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _isConnected = false;
            _logger.LogError(ex, "SignalR: Unauthorized - token expired?");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            {
                State = HubConnectionState.Disconnected,
                Error = "Unauthorized"
            });
            OnAuthenticationFailed?.Invoke(this, EventArgs.Empty);
            throw new UnauthorizedAccessException("Authentication failed. Please login again.", ex);
        }
        catch (OperationCanceledException)
        {
            _isConnected = false;
            _logger.LogWarning("SignalR: Connection cancelled");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            {
                State = HubConnectionState.Disconnected,
                Error = "Cancelled"
            });
        }
        catch (Exception ex)
        {
            _isConnected = false;
            _logger.LogError(ex, "SignalR: Failed to start connection");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            {
                State = HubConnectionState.Disconnected,
                Error = ex.Message
            });
            throw;
        }
        finally
        {
            _isConnecting = false;
            _connectionLock.Release();
        }
    }

    public async Task StopConnection()
    {
        await _connectionLock.WaitAsync();
        try
        {
            _logger.LogInformation("SignalR: Stopping connection...");

            // Останавливаем heartbeat
            StopHeartbeat();

            if (_hubConnection != null)
            {
                if (_hubConnection.State != HubConnectionState.Disconnected)
                {
                    await _hubConnection.StopAsync();
                }

                await _hubConnection.DisposeAsync();
                _hubConnection = null;
            }

            _isConnected = false;
            _reconnectAttempts = 0;

            // Очищаем подписки
            UnsubscribeAll();

            // Очищаем очередь (но не теряем сообщения, просто отложим)
            _logger.LogInformation($"SignalR: Connection stopped, {_messageQueue.Count} messages queued");

            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            {
                State = HubConnectionState.Disconnected
            });
        }
        finally
        {
            _connectionLock.Release();
        }
    }

    public async Task SendMessageAsync(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (message.Length > 500)
        {
            OnErrorReceived?.Invoke(this, "Message too long (max 500 chars)");
            return;
        }

        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var nonce = Guid.NewGuid().ToString("N");

        if (_hubConnection?.State == HubConnectionState.Connected && _isConnected)
        {
            try
            {
                await _hubConnection.SendAsync("SendMessage", message, null, timestamp, nonce, _cts.Token);
                _logger.LogInformation("✅ Message sent immediately");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Send error, queueing message");
                _messageQueue.Enqueue(new QueuedMessage
                {
                    Content = message,
                    Timestamp = timestamp,
                    Nonce = nonce,
                    ActionType = null
                });
                OnErrorReceived?.Invoke(this, "Message queued - will send when reconnected");
            }
        }
        else
        {
            _logger.LogWarning("SignalR: Not connected, queueing message");
            _messageQueue.Enqueue(new QueuedMessage
            {
                Content = message,
                Timestamp = timestamp,
                Nonce = nonce,
                ActionType = null
            });

            // Пробуем подключиться
            _ = Task.Run(async () =>
            {
                await Task.Delay(500);
                if (!_isConnected && !_isConnecting)
                {
                    try
                    {
                        await StartConnection();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to connect while queueing message");
                    }
                }
            });
        }
    }

    public async Task SendAnimationAsync(string animationType)
    {
        if (string.IsNullOrWhiteSpace(animationType))
            return;

        var validAnimations = new[] { "clap", "wave", "dance", "laugh", "applause" };
        if (!validAnimations.Contains(animationType.ToLower()))
        {
            _logger.LogWarning($"Invalid animation type: {animationType}");
            return;
        }

        try
        {
            if (_hubConnection?.State == HubConnectionState.Connected && _isConnected)
            {
                _logger.LogInformation($"SignalR: Sending animation: {animationType}");
                await _hubConnection.InvokeAsync("SendAnimation", animationType, _cts.Token);
            }
            else
            {
                _logger.LogWarning($"SignalR: Cannot send animation, state is {_hubConnection?.State}");
                OnErrorReceived?.Invoke(this, "Cannot send animation - not connected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"SignalR SendAnimation Error: {animationType}");
            OnErrorReceived?.Invoke(this, $"Failed to send animation: {ex.Message}");
        }
    }

    public async Task SendTypingIndicatorAsync()
    {
        try
        {
            if (_hubConnection?.State == HubConnectionState.Connected && _isConnected)
            {
                await _hubConnection.InvokeAsync("UserTyping", _cts.Token);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SignalR Typing Indicator Error");
            // Не показываем ошибку пользователю - это не критично
        }
    }

    #endregion

    #region Приватные методы

    private async Task<HubConnection> CreateConnectionAsync()
    {
        _logger.LogInformation("🔧 Creating new HubConnection");

        // Получаем валидный токен
        var accessToken = await _tokenManager.GetValidTokenAsync(_authService);
        if (string.IsNullOrEmpty(accessToken))
        {
            throw new InvalidOperationException("No valid access token available");
        }

        _logger.LogInformation($"SignalR: Creating connection with valid token. Length: {accessToken.Length}");

        var connection = new HubConnectionBuilder()
            .WithUrl(_hubUrl, options =>
            {
                options.AccessTokenProvider = async () =>
                {
                    var token = await _tokenManager.GetValidTokenAsync(_authService);
                    _logger.LogDebug($"SignalR: Token provided, exists: {!string.IsNullOrEmpty(token)}");
                    return token;
                };
                options.Transports = HttpTransportType.WebSockets;
                options.SkipNegotiation = true;
                options.CloseTimeout = TimeSpan.FromSeconds(5);
            })
            .WithAutomaticReconnect(new[]
            {
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(10),
                TimeSpan.FromSeconds(20),
                TimeSpan.FromSeconds(30)
            })
            .ConfigureLogging(logging =>
            {
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Debug);
            })
            .Build();

        // Подписываемся на событие Closed и сохраняем делегат
        _connectionClosedHandler = OnConnectionClosed;
        connection.Closed += _connectionClosedHandler;

        // Подписываемся на сообщения
        _subscriptions.Add(connection.On<ChatMessageDto>("ReceiveMessage", OnReceiveMessage));
        _subscriptions.Add(connection.On<UserJoinedData>("UserJoined", OnUserJoinedHandler));
        _subscriptions.Add(connection.On<IEnumerable<UserJoinedData>>("ReceiveInitialPresence", OnInitialPresenceHandler));
        _subscriptions.Add(connection.On<RoomAssignmentData>("AssignedToRoom", OnAssignedToRoomHandler));
        _subscriptions.Add(connection.On<UserLeftData>("UserLeft", OnUserLeftHandler));
        _subscriptions.Add(connection.On<UserTypingData>("UserTyping", OnUserTypingHandler));
        _subscriptions.Add(connection.On<string>("ReceiveError", OnErrorHandler));
        _subscriptions.Add(connection.On<string, string>("UserPlayAnimation", OnUserPlayAnimationHandler));

        return connection;
    }

    private async Task DisposeOldConnectionAsync()
    {
        if (_hubConnection != null)
        {
            try
            {
                // Отписываемся от события Closed
                if (_connectionClosedHandler != null)
                {
                    _hubConnection.Closed -= _connectionClosedHandler;
                    _connectionClosedHandler = null;
                }

                // Remove-методы есть, но они не обязательны, т.к. соединение будет уничтожено
                _hubConnection.Remove("ReceiveMessage");
                _hubConnection.Remove("UserJoined");
                _hubConnection.Remove("ReceiveInitialPresence");
                _hubConnection.Remove("AssignedToRoom");
                _hubConnection.Remove("UserLeft");
                _hubConnection.Remove("UserTyping");
                _hubConnection.Remove("ReceiveError");
                _hubConnection.Remove("UserPlayAnimation");

                if (_hubConnection.State != HubConnectionState.Disconnected)
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                    await _hubConnection.StopAsync(cts.Token);
                }

                await _hubConnection.DisposeAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing old connection");
            }
            finally
            {
                _hubConnection = null;
            }
        }
    }

    private void UnsubscribeAll()
    {
        // Отписываемся от сообщений (IDisposable)
        foreach (var subscription in _subscriptions)
        {
            try
            {
                subscription?.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing subscription");
            }
        }
        _subscriptions.Clear();

        // Отписываемся от события Closed
        if (_hubConnection != null && _connectionClosedHandler != null)
        {
            try
            {
                _hubConnection.Closed -= _connectionClosedHandler;
                _connectionClosedHandler = null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unsubscribing from Closed event");
            }
        }
    }

    private async Task OnConnectionClosed(Exception? error)
    {
        _isConnected = false;
        StopHeartbeat();

        _logger.LogWarning($"SignalR Connection closed: {error?.Message}");
        OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
        {
            State = HubConnectionState.Disconnected,
            Error = error?.Message
        });

        // Если 401 - пробуем обновить токен
        if (error?.Message?.Contains("401") == true ||
            error?.Message?.Contains("Unauthorized") == true)
        {
            try
            {
                _logger.LogInformation("SignalR: 401 detected, refreshing token...");
                await _tokenManager.GetValidTokenAsync(_authService);
                await TryReconnectWithBackoff();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SignalR: Failed to refresh token");
                OnAuthenticationFailed?.Invoke(this, EventArgs.Empty);
            }
        }
        else
        {
            // Обычная ошибка - пробуем переподключиться
            await TryReconnectWithBackoff();
        }
    }

    private async Task TryReconnectWithBackoff()
    {
        if (_reconnectAttempts >= MaxReconnectAttempts)
        {
            _logger.LogWarning($"SignalR: Max reconnect attempts ({MaxReconnectAttempts}) reached");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            {
                State = HubConnectionState.Disconnected,
                Error = "Max reconnect attempts exceeded"
            });
            return;
        }

        if (!await _reconnectLock.WaitAsync(TimeSpan.FromSeconds(1)))
        {
            _logger.LogWarning("SignalR: Reconnect already in progress");
            return;
        }

        try
        {
            _reconnectAttempts++;

            // Exponential backoff: 2^attempt * base delay
            var delayMs = Math.Pow(2, _reconnectAttempts) * ReconnectBaseDelayMs;
            delayMs = Math.Min(delayMs, 30000); // Максимум 30 секунд

            _logger.LogInformation($"SignalR: Reconnect attempt {_reconnectAttempts}/{MaxReconnectAttempts} in {delayMs}ms");

            await Task.Delay((int)delayMs, _cts.Token);

            if (!_cts.IsCancellationRequested)
            {
                await StartConnection();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("SignalR: Reconnect cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"SignalR: Reconnect attempt {_reconnectAttempts} failed");

            // Пробуем снова через Exponential backoff
            _ = Task.Delay(1000).ContinueWith(async _ =>
            {
                await TryReconnectWithBackoff();
            });
        }
        finally
        {
            _reconnectLock.Release();
        }
    }
    
    #endregion

    #region Обработчики событий SignalR

    private void OnReceiveMessage(ChatMessageDto message)
    {
        try
        {
            _logger.LogDebug("SignalR: Message received from {Sender}: {Content}",
                message.SenderNickname,
                message.Content?.Length > 50 ? message.Content.Substring(0, 50) + "..." : message.Content);

            var chatMessage = new ChatMessage
            {
                SenderId = message.SenderId.ToString(),
                SenderNickname = message.SenderNickname,
                Content = message.Content,
                SentAt = message.SentAt,
                IsCurrentUser = message.SenderId.ToString() == _currentUserId
            };

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    OnMessageReceived?.Invoke(this, chatMessage);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error invoking OnMessageReceived");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing ReceiveMessage");
        }
    }

    private void OnUserJoinedHandler(UserJoinedData userData)
    {
        try
        {
            _logger.LogInformation($"SignalR: User joined: {userData?.Nickname}");

            if (userData != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        OnUserJoined?.Invoke(this, userData);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in OnUserJoined handler");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserJoined");
        }
    }

    private void OnInitialPresenceHandler(IEnumerable<UserJoinedData> users)
    {
        try
        {
            var userList = users?.ToList() ?? new List<UserJoinedData>();
            _logger.LogInformation($"SignalR: Received {userList.Count} existing users");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    OnInitialPresenceReceived?.Invoke(this, userList);
                    OnInitialPresenceData?.Invoke(this, userList);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OnInitialPresence handler");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing InitialPresence");
        }
    }

    private void OnAssignedToRoomHandler(RoomAssignmentData data)
    {
        try
        {
            _logger.LogInformation($"SignalR: Assigned to room {data.RoomName}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    OnAssignedToRoom?.Invoke(this, data);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OnAssignedToRoom handler");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing AssignedToRoom");
        }
    }

    private void OnUserLeftHandler(UserLeftData data)
    {
        try
        {
            _logger.LogInformation($"SignalR: User left: {data?.UserId}");

            if (data != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        OnUserLeft?.Invoke(this, data);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in OnUserLeft handler");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserLeft");
        }
    }

    private void OnUserTypingHandler(UserTypingData data)
    {
        try
        {
            _logger.LogDebug($"SignalR: User typing: {data?.Nickname}");

            if (data != null)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    try
                    {
                        OnUserTyping?.Invoke(this, data.UserId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in OnUserTyping handler");
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserTyping");
        }
    }

    private void OnErrorHandler(string error)
    {
        try
        {
            _logger.LogError($"SignalR Error: {error}");

            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    OnErrorReceived?.Invoke(this, error);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in OnError handler");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Error message");
        }
    }

    private void OnUserPlayAnimationHandler(string userId, string animationType)
    {
        try
        {
            _logger.LogInformation($"SignalR: User {userId} played animation {animationType}");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var webViewService = MauiProgram.ServiceProvider.GetRequiredService<WebViewService>();
                    await webViewService.PlayAnimationAsync(userId, animationType, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error playing animation");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserPlayAnimation");
        }
    }

    #endregion

    #region Heartbeat

    private void StartHeartbeat()
    {
        StopHeartbeat();
        _heartbeatTimer = new Timer(DoHeartbeat, null, _heartbeatInterval, _heartbeatInterval);
        _logger.LogDebug("SignalR: Heartbeat started");
    }

    private void StopHeartbeat()
    {
        _heartbeatTimer?.Dispose();
        _heartbeatTimer = null;
    }

    private async void DoHeartbeat(object state)
    {
        if (!_isConnected || _hubConnection?.State != HubConnectionState.Connected)
            return;

        try
        {
            using var cts = new CancellationTokenSource(_heartbeatTimeout);
            await _hubConnection.InvokeAsync("GetOnlineCount", cts.Token);
            _logger.LogTrace("SignalR: Heartbeat OK");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SignalR: Heartbeat failed");

            // Если heartbeat упал - пробуем переподключиться
            if (_isConnected)
            {
                _isConnected = false;
                _ = Task.Run(async () => await TryReconnectWithBackoff());
            }
        }
    }

    #endregion

    #region Очередь сообщений

    private async void ProcessMessageQueue(object state)
    {
        if (!_isConnected || _hubConnection?.State != HubConnectionState.Connected)
            return;

        while (_messageQueue.TryDequeue(out var queuedMessage))
        {
            try
            {
                // Проверяем, не устарело ли сообщение
                var now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                if (now - queuedMessage.Timestamp > 60)
                {
                    _logger.LogWarning("Dropping stale message from queue");
                    continue;
                }

                await _hubConnection.SendAsync("SendMessage",
                    queuedMessage.Content,
                    queuedMessage.ActionType,
                    queuedMessage.Timestamp,
                    queuedMessage.Nonce,
                    _cts.Token);

                _logger.LogInformation("✅ Queued message sent");
                await Task.Delay(100); // Небольшая пауза между сообщениями
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send queued message, re-queueing");
                _messageQueue.Enqueue(queuedMessage);
                break; // Выходим, если не получилось отправить
            }
        }
    }

    #endregion

    #region IDisposable

    private bool _disposed;

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("SignalRService: Disposing...");

        // Отменяем все операции
        _cts.Cancel();

        // Останавливаем таймеры
        StopHeartbeat();
        _queueProcessorTimer?.Dispose();

        // Отписываемся от всего
        UnsubscribeAll();

        // Закрываем соединение
        if (_hubConnection != null)
        {
            try
            {
                // Отписываемся от события Closed еще раз для надежности
                if (_connectionClosedHandler != null)
                {
                    _hubConnection.Closed -= _connectionClosedHandler;
                    _connectionClosedHandler = null;
                }

                if (_hubConnection.State != HubConnectionState.Disconnected)
                {
                    _hubConnection.StopAsync().Wait(1000);
                }
                _hubConnection.DisposeAsync().AsTask().Wait(1000);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing connection");
            }
            _hubConnection = null;
        }

        // Освобождаем семафоры
        _connectionLock?.Dispose();
        _reconnectLock?.Dispose();
        _cts?.Dispose();

        _disposed = true;
        _logger.LogInformation("SignalRService: Disposed");
    }

    #endregion

    #region Вспомогательные классы

    private class ChatMessageDto
    {
        public Guid Id { get; set; }
        public Guid SenderId { get; set; }
        public string SenderNickname { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset SentAt { get; set; }
        public string? ActionType { get; set; }
    }

    private class QueuedMessage
    {
        public string Content { get; set; }
        public string? ActionType { get; set; }
        public long Timestamp { get; set; }
        public string Nonce { get; set; }
    }

    public class ConnectionStateChangedEventArgs : EventArgs
    {
        public HubConnectionState State { get; set; }
        public string? Error { get; set; }
    }

    public class UserLeftEventArgs : EventArgs
    {
        public string UserId { get; set; }
        public string Nickname { get; set; }
        public DateTimeOffset LeftAt { get; set; }
    }

    #endregion
}