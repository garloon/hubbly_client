using Hubbly.Mobile.Config;
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
    private readonly WebViewService _webViewService;
    private readonly ILogger<SignalRService> _logger;

    // Synchronization
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly SemaphoreSlim _reconnectLock = new(1, 1);

    // Connection
    private HubConnection _hubConnection;
    private readonly List<IDisposable> _subscriptions = new();
    private readonly CancellationTokenSource _cts = new();
    private string _hubUrl;
    
    private Func<Exception?, Task> _connectionClosedHandler;

    // State
    private bool _isConnecting = false;
    private bool _isConnected = false;
    private DateTime _lastConnectionAttempt = DateTime.MinValue;

    // Reconnect
    private int _reconnectAttempts = 0;
    private const int MaxReconnectAttempts = 5;
    private const int ReconnectBaseDelayMs = 2000;

    // Heartbeat
    private readonly IHeartbeatService _heartbeatService;
    private string _currentUserId;

    // Message queue for offline mode
    private readonly ConcurrentQueue<QueuedMessage> _messageQueue = new();
    private readonly Timer _queueProcessorTimer;

    // Events
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
    public event EventHandler<(string userId, string animationType)> OnUserPlayAnimation;

    // Properties
    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected && _isConnected;
    public HubConnectionState ConnectionState => _hubConnection?.State ?? HubConnectionState.Disconnected;

    public SignalRService(TokenManager tokenManager, AuthService authService, WebViewService webViewService, IHeartbeatService heartbeatService, ILogger<SignalRService> logger)
    {
        _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _webViewService = webViewService ?? throw new ArgumentNullException(nameof(webViewService));
        _heartbeatService = heartbeatService ?? throw new ArgumentNullException(nameof(heartbeatService));
        _logger = logger;

        // Read server URL from Preferences via ServerConfig
        var serverUrl = ServerConfig.GetServerUrl();
        _hubUrl = $"{serverUrl.TrimEnd('/')}/chatHub";
        
        _logger.LogInformation("🚀 SignalRService created with URL: {Url}", _hubUrl);

        // Timer for processing message queue
        _queueProcessorTimer = new Timer(ProcessMessageQueue, null,
            TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));

        // Configure heartbeat service
        _heartbeatService.Interval = TimeSpan.FromSeconds(30);
        _heartbeatService.Timeout = TimeSpan.FromSeconds(10);
        _heartbeatService.HeartbeatSucceeded += OnHeartbeatSucceeded;
        _heartbeatService.HeartbeatFailed += OnHeartbeatFailed;

        // NOTE: _currentUserId will be loaded on-demand when needed.
        // Removed fire-and-forget to avoid race conditions.
    }

    #region Public Methods

    public async Task StartConnection()
    {
        _logger.LogInformation("📞 StartConnection called. Current state: {State}", _hubConnection?.State);
        // Protection against too frequent attempts
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

            // Load current user ID before connecting
            if (string.IsNullOrEmpty(_currentUserId))
            {
                _currentUserId = await _tokenManager.GetAsync("user_id");
                _logger.LogDebug("SignalR: Loaded current user ID: {UserId}", _currentUserId);
            }

            // Dispose old connection if exists
            await DisposeOldConnectionAsync();

            // Create new connection
            _hubConnection = await CreateConnectionAsync();

            // Start
            await _hubConnection.StartAsync(_cts.Token);

            _isConnected = true;
            _reconnectAttempts = 0;

            // Start heartbeat
            _heartbeatService.StartAsync();

            _logger.LogInformation($"SignalR: Connected successfully. State: {_hubConnection.State}");
            OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
            {
                State = _hubConnection.State
            });

            // Send queued messages
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

            // Stop heartbeat
            _heartbeatService.StopAsync();

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
            _lastConnectionAttempt = DateTime.MinValue;

            // Clear subscriptions
            UnsubscribeAll();

            // Clear queue (but don't lose messages, just defer)
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

            // Try to connect
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

    public async Task SendAnimationAsync(string animationType, string? targetUserId = null)
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
                _logger.LogInformation($"SignalR: Sending animation: {animationType}, target: {targetUserId ?? "self"}");
                await _hubConnection.InvokeAsync("SendAnimation", animationType, targetUserId, _cts.Token);
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
            // Don't show error to user - not critical
        }
    }

    #endregion

    #region Private Methods

    private async Task<HubConnection> CreateConnectionAsync()
    {
        _logger.LogInformation("🔧 Creating new HubConnection");

        // Get valid token
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

        // Subscribe to Closed event and save delegate
        _connectionClosedHandler = OnConnectionClosed;
        connection.Closed += _connectionClosedHandler;

        // Subscribe to messages
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
                // Unsubscribe from Closed event
                if (_connectionClosedHandler != null)
                {
                    _hubConnection.Closed -= _connectionClosedHandler;
                    _connectionClosedHandler = null;
                }

                // Remove methods exist but are not required as connection will be destroyed
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
        // Unsubscribe from messages (IDisposable)
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

        // Unsubscribe from Closed event
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
        _heartbeatService.StopAsync();

        _logger.LogWarning($"SignalR Connection closed: {error?.Message}");
        OnConnectionStateChanged?.Invoke(this, new ConnectionStateChangedEventArgs
        {
            State = HubConnectionState.Disconnected,
            Error = error?.Message
        });

        // If 401 - try to refresh token
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
            // Regular error - try to reconnect
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
            delayMs = Math.Min(delayMs, 30000); // Maximum 30 seconds

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

            // Try again with exponential backoff
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

    #region SignalR Event Handlers

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
            
            // Raise event for ViewModels to handle
            OnUserPlayAnimation?.Invoke(this, (userId, animationType));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing UserPlayAnimation");
        }
    }

    #endregion

    private void OnHeartbeatSucceeded(object? sender, EventArgs e)
    {
        _logger.LogTrace("SignalR: Heartbeat OK");
    }

    private void OnHeartbeatFailed(object? sender, string error)
    {
        _logger.LogWarning($"SignalR: Heartbeat failed: {error}");

        // If heartbeat failed - try to reconnect
        if (_isConnected)
        {
            _isConnected = false;
            _ = Task.Run(async () => await TryReconnectWithBackoff());
        }
    }

    #region Message queue

    private async void ProcessMessageQueue(object state)
    {
        if (!_isConnected || _hubConnection?.State != HubConnectionState.Connected)
            return;

        while (_messageQueue.TryDequeue(out var queuedMessage))
        {
            try
            {
                // Check if message is stale
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
                await Task.Delay(100); // Small pause between messages
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send queued message, re-queueing");
                _messageQueue.Enqueue(queuedMessage);
                break; // Exit if failed to send
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

        // Cancel all operations
        _cts.Cancel();

        // Stop timers
        _heartbeatService.StopAsync();
        _queueProcessorTimer?.Dispose();

        // Unsubscribe from everything
        UnsubscribeAll();

        // Close connection
        if (_hubConnection != null)
        {
            try
            {
                // Unsubscribe from Closed event again for reliability
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

        // Dispose semaphores
        _connectionLock?.Dispose();
        _reconnectLock?.Dispose();
        _cts?.Dispose();

        _disposed = true;
        _logger.LogInformation("SignalRService: Disposed");
    }

    #endregion

    #region Helper Classes

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