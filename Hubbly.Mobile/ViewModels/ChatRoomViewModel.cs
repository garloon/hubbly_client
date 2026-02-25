using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hubbly.Mobile.Models;
using Hubbly.Mobile.Services;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Text.Json;
using System.Text.Json.Serialization;
using static Hubbly.Mobile.Services.SignalRService;

namespace Hubbly.Mobile.ViewModels;

public partial class ChatRoomViewModel : ObservableObject, IDisposable, IAsyncDisposable, IQueryAttributable
{
    private readonly ILogger<ChatRoomViewModel> _logger;
    private readonly SignalRService _signalRService;
    private readonly WebViewService _webViewService;
    private readonly INavigationService _navigationService;
    private readonly TokenManager _tokenManager;
    private readonly AuthService _authService;
    private readonly SemaphoreSlim _connectionLock = new(1, 1);
    private readonly SemaphoreSlim _avatarLock = new(1, 1);
    private readonly SemaphoreSlim _messageLock = new(1, 1);
    private readonly SemaphoreSlim _syncLock = new(1, 1); // For sync/update operations
    private readonly CancellationTokenSource _cts = new();
    private readonly HashSet<string> _processedUserIds = new();
    private readonly Debouncer _typingDebouncer;
    private readonly Debouncer _presenceDebouncer;

    private bool _disposed;
    private DateTime _lastTypingTime = DateTime.MinValue;
    private bool _isLeaving;

    // Observable Properties
    [ObservableProperty]
    private string _messageText = string.Empty;

    [ObservableProperty]
    private string _userId = string.Empty;

    [ObservableProperty]
    private string _nickname = "Guest";

    [ObservableProperty]
    private bool _isTypingVisible = false;

    [ObservableProperty]
    private bool _isConnected = false;

    [ObservableProperty]
    private bool _isInitialPresenceLoaded = false;

    [ObservableProperty]
    private ObservableCollection<ChatMessage> _messages = new();

    [ObservableProperty]
    private ObservableCollection<AvatarPresence> _onlineAvatars = new();

    [ObservableProperty]
    private AvatarPresence? _selectedAvatar;

    [ObservableProperty]
    private bool _isControlPanelVisible = false;

    [ObservableProperty]
    private string _roomName = "Connecting...";

    [ObservableProperty]
    private string _roomStatus = "0/50";

    [ObservableProperty]
    private int _usersInRoom;

    [ObservableProperty]
    private int _maxUsers = 50;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _is3DEnabled = true;

    [ObservableProperty]
    private bool _useFallbackAvatars = false;

    [ObservableProperty]
    private string _connectionError = string.Empty;

    [ObservableProperty]
    private bool _hasConnectionError;

    [ObservableProperty]
    private AvatarConfigDto _currentUserAvatar;

    [ObservableProperty]
    private bool _canNavigateLeft = false;

    [ObservableProperty]
    private bool _canNavigateRight = false;

    [ObservableProperty]
    private bool _isHoldNavigationActive = false;

    public ChatRoomViewModel(
        SignalRService signalRService,
        WebViewService webViewService,
        INavigationService navigationService,
        TokenManager tokenManager,
        AuthService authService,
        ILogger<ChatRoomViewModel> logger)
    {
        _signalRService = signalRService ?? throw new ArgumentNullException(nameof(signalRService));
        _webViewService = webViewService ?? throw new ArgumentNullException(nameof(webViewService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        _logger = logger;

        _typingDebouncer = new Debouncer(TimeSpan.FromSeconds(1), SendTypingIndicatorInternal);
        _presenceDebouncer = new Debouncer(TimeSpan.FromMilliseconds(500), UpdatePresenceInternal);

        // Subscribe to SignalR events
        InitializeSignalREvents();

        _logger.LogInformation("ChatRoomViewModel created");
    }

    #region Initialization and Data Loading

    private async Task LoadUserDataAsync()
    {
        try
        {
            _logger.LogInformation("Loading user data");

            // Load user ID
            _userId = await _tokenManager.GetAsync("user_id") ?? string.Empty;
            _logger.LogInformation("User ID from TokenManager: '{UserId}'", _userId);
            
            if (string.IsNullOrEmpty(_userId))
            {
                _logger.LogWarning("User ID is empty, checking encrypted storage");
                _userId = await _tokenManager.GetEncryptedAsync("user_id");
                _logger.LogInformation("User ID from encrypted storage: '{UserId}'", _userId);
            }

            // Load nickname
            _nickname = await _tokenManager.GetNicknameAsync();
            _logger.LogInformation("Nickname: '{Nickname}'", _nickname);
            
            OnPropertyChanged(nameof(Nickname));

            // Load avatar config (encrypted)
            var avatarConfigJson = await _tokenManager.GetEncryptedAsync("avatar_config") ?? "{}";
            if (!string.IsNullOrEmpty(avatarConfigJson) && avatarConfigJson != "{}")
            {
                try
                {
                    CurrentUserAvatar = AvatarConfigDto.FromJson(avatarConfigJson);
                    _logger.LogInformation("Avatar config loaded: {Gender}", CurrentUserAvatar?.Gender);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to load avatar config");
                    CurrentUserAvatar = new AvatarConfigDto { Gender = "male" };
                }
            }
            else
            {
                _logger.LogWarning("No avatar config found, using default");
                CurrentUserAvatar = new AvatarConfigDto { Gender = "male" };
            }

            // Add self to presence list
            if (Guid.TryParse(_userId, out var userIdGuid))
            {
                var selfPresence = new AvatarPresence
                {
                    UserId = userIdGuid,
                    Nickname = _nickname,
                    AvatarConfig = CurrentUserAvatar,
                    JoinedAt = DateTime.UtcNow,
                    IsCurrentUser = true
                };

                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnlineAvatars.Add(selfPresence);
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading user data");
            throw; // Re-throw to be caught by OnAppearing
        }
    }

    private void InitializeSignalREvents()
    {
        try
        {
            _logger.LogDebug("Initializing SignalR events");

            _signalRService.OnMessageReceived += OnMessageReceived;
            _signalRService.OnAssignedToRoom += OnAssignedToRoom;
            _signalRService.OnUserJoined += OnUserJoined;
            _signalRService.OnUserLeft += OnUserLeft;
            _signalRService.OnUserTyping += OnUserTyping;
            _signalRService.OnInitialPresenceReceived += OnInitialPresenceReceived;
            _signalRService.OnErrorReceived += OnErrorReceived;
            _signalRService.OnAuthenticationFailed += OnAuthenticationFailed;
            _signalRService.OnConnectionStateChanged += OnConnectionStateChanged;
            _signalRService.OnUserPlayAnimation += OnUserPlayAnimation;

            // DISABLED: Avatar click events - using MAUI controls only
            // _webViewService.OnAvatarClicked += OnAvatarClicked;
            _webViewService.OnSceneReady += OnSceneReady;
            _webViewService.OnSceneError += OnSceneError;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing SignalR events");
        }
    }

    private void UnsubscribeSignalREvents()
    {
        try
        {
            _logger.LogInformation("Unsubscribing from SignalR events");

            _signalRService.OnMessageReceived -= OnMessageReceived;
            _signalRService.OnUserJoined -= OnUserJoined;
            _signalRService.OnUserLeft -= OnUserLeft;
            _signalRService.OnUserTyping -= OnUserTyping;
            _signalRService.OnAssignedToRoom -= OnAssignedToRoom;
            _signalRService.OnInitialPresenceReceived -= OnInitialPresenceReceived;
            _signalRService.OnErrorReceived -= OnErrorReceived;
            _signalRService.OnAuthenticationFailed -= OnAuthenticationFailed;
            _signalRService.OnConnectionStateChanged -= OnConnectionStateChanged;
            _signalRService.OnUserPlayAnimation -= OnUserPlayAnimation;

            // DISABLED: Avatar click events - using MAUI controls only
            // _webViewService.OnAvatarClicked -= OnAvatarClicked;
            _webViewService.OnSceneReady -= OnSceneReady;
            _webViewService.OnSceneError -= OnSceneError;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from SignalR events");
        }
    }

    #endregion

    #region Commands

    [RelayCommand]
    public async Task ConnectToChat()
    {
        _logger.LogInformation("🔵 ConnectToChat started");
        if (_isLeaving) return;

        if (_signalRService.IsConnected)
        {
            _logger.LogInformation("Already connected");
            IsConnected = true;
            return;
        }

        if (IsBusy)
        {
            _logger.LogInformation("Already busy connecting");
            return;
        }

        if (!await _connectionLock.WaitAsync(TimeSpan.FromSeconds(10)))
        {
            _logger.LogError("Failed to acquire connection lock");
            ConnectionError = "System busy, please try again";
            HasConnectionError = true;
            return;
        }

        try
        {
            IsBusy = true;
            ConnectionError = string.Empty;
            HasConnectionError = false;

            _logger.LogInformation("Connecting to chat...");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(30));

            await _signalRService.StartConnection();

            IsConnected = true;

            // Add self to 3D scene
            _ = Task.Run(AddSelfTo3DScene);
        }
        catch (UnauthorizedAccessException)
        {
            _logger.LogWarning("Authentication failed - redirecting");
            ConnectionError = "Session expired. Please login again.";
            HasConnectionError = true;

            await Shell.Current.DisplayAlert(
                "Session Expired",
                "Your session has expired. Please login again.",
                "OK");

            await _navigationService.NavigateToAsync("//WelcomePage");
        }
        catch (TimeoutException ex)
        {
            _logger.LogError(ex, "Connection timeout");
            ConnectionError = "Connection timeout. Please check your internet.";
            HasConnectionError = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection error");
            ConnectionError = $"Failed to connect: {ex.Message}";
            HasConnectionError = true;
        }
        finally
        {
            IsBusy = false;
            _connectionLock.Release();
        }
    }

    [RelayCommand]
    public async Task DisconnectFromChat()
    {
        if (_isLeaving) return;

        if (!await _connectionLock.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            _logger.LogError("Failed to acquire connection lock for disconnect");
            return;
        }

        try
        {
            _isLeaving = true;
            IsBusy = true;

            _logger.LogInformation("Disconnecting from chat...");

            // Clear 3D scene
            if (_is3DEnabled)
            {
                await _webViewService.ClearAvatarsAsync();
            }

            await _signalRService.StopConnection();

            IsConnected = false;
            IsInitialPresenceLoaded = false;

            // Clear collections
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Messages.Clear();
                OnlineAvatars.Clear();
                _processedUserIds.Clear();
            });

            _logger.LogInformation("Disconnected and cleared data");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting");
        }
        finally
        {
            IsBusy = false;
            _isLeaving = false;
            _connectionLock.Release();
        }
    }

    [RelayCommand]
    private async Task SendMessage()
    {
        if (string.IsNullOrWhiteSpace(MessageText))
            return;

        if (!IsConnected)
        {
            ConnectionError = "Not connected to chat";
            HasConnectionError = true;
            return;
        }

        if (!await _messageLock.WaitAsync(TimeSpan.FromSeconds(2)))
        {
            _logger.LogWarning("Failed to acquire message lock");
            return;
        }

        try
        {
            var messageToSend = MessageText.Trim();
            
            // Vibration feedback - with error handling
            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Vibrate>();
                if (status == PermissionStatus.Granted)
                {
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(30));
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Vibration not available");
            }

            HideKeyboard();

            // Clear the message field immediately
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                MessageText = string.Empty;
            });

            await _signalRService.SendMessageAsync(messageToSend);

            _logger.LogDebug("Message sent: {Message}", messageToSend.Substring(0, Math.Min(20, messageToSend.Length)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message");
            ConnectionError = "Failed to send message";
            HasConnectionError = true;
        }
        finally
        {
            _messageLock.Release();
        }
    }

    [RelayCommand]
    private async Task SendAnimation(string animationType)
    {
        if (!IsConnected)
        {
            _logger.LogWarning("Cannot send animation - not connected");
            return;
        }

        try
        {
            // Send to all users (including self) via SignalR
            _logger.LogInformation("Sending animation: {AnimationType}", animationType);
            await _signalRService.SendAnimationAsync(animationType, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending animation");
            await Shell.Current.DisplayAlert("Error", "Failed to send animation", "OK");
        }
    }

    [RelayCommand]
    public async Task SendTypingIndicator()
    {
        if (!IsConnected) return;

        try
        {
            _typingDebouncer.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in typing indicator");
        }
    }

    [RelayCommand]
    private async Task RetryConnection()
    {
        ConnectionError = string.Empty;
        HasConnectionError = false;
        await ConnectToChat();
    }

    [RelayCommand]
    private async Task LeaveChat()
    {
        if (_isLeaving) return;

        var confirm = await Shell.Current.DisplayAlert(
            "Leave Chat",
            "Are you sure you want to leave?",
            "Yes", "No");

        if (confirm)
        {
            await DisconnectFromChat();
            await _navigationService.GoBackAsync();
        }
    }

    [RelayCommand]
    private void ToggleFlyout()
    {
        try
        {
            if (Application.Current?.MainPage is Shell shell)
            {
                shell.FlyoutIsPresented = !shell.FlyoutIsPresented;
                _logger.LogDebug("Flyout toggled. IsPresented: {IsPresented}", shell.FlyoutIsPresented);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling flyout");
        }
    }

    #endregion

    #region SignalR Event Handlers

    private void OnMessageReceived(object sender, ChatMessage message)
    {
        try
        {
            // Устанавливаем IsCurrentUser для корректного отображения
            message.IsCurrentUser = message.SenderId == _userId;
            
            MainThread.BeginInvokeOnMainThread(() =>
            {
                try
                {
                    Messages.Add(message);
                    _logger.LogDebug("Message added: {Sender}: {Content}, IsCurrentUser: {IsCurrentUser}",
                        message.SenderNickname,
                        message.Content?.Length > 30 ? message.Content.Substring(0, 30) + "..." : message.Content,
                        message.IsCurrentUser);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error adding message to UI");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling received message");
        }
    }

    private async void OnUserJoined(object sender, UserJoinedData userData)
    {
        if (userData == null) return;

        try
        {
            _logger.LogInformation("User joined: {Nickname} ({UserId})",
                userData.Nickname, userData.UserId);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                // Update counter
                UsersInRoom++;
                RoomStatus = $"{UsersInRoom}/{MaxUsers}";

                // Add system message
                Messages.Add(new ChatMessage
                {
                    SenderId = "system",
                    SenderNickname = "System",
                    Content = $"🎉 {userData.Nickname} joined",
                    SentAt = DateTimeOffset.UtcNow,
                    IsCurrentUser = false
                });

                // Get gender from avatar config
                string gender = "male";
                try
                {
                    if (!string.IsNullOrEmpty(userData.AvatarConfigJson))
                    {
                        var config = AvatarConfigDto.FromJson(userData.AvatarConfigJson);
                        gender = config.Gender;
                    }
                }
                catch
                {
                    gender = "male";
                }

                // Add to 3D scene
                await Ensure3DAvatarPresence(userData.UserId, userData.Nickname, gender);
                
                // Sync avatars from 3D scene and update selection
                await SyncAvatarsAndUpdateSelection();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling user joined");
        }
    }

    private async void OnUserLeft(object sender, UserLeftData data)
    {
        try
        {
            _logger.LogInformation("User left: {Nickname} ({UserId})", data.Nickname, data.UserId);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                UsersInRoom = Math.Max(0, UsersInRoom - 1);
                RoomStatus = $"{UsersInRoom}/{MaxUsers}";

                // Remove from presence list
                var avatar = OnlineAvatars.FirstOrDefault(a => a.UserId.ToString() == data.UserId);
                if (avatar != null)
                {
                    OnlineAvatars.Remove(avatar);
                }

                // Remove from processed users set to allow re-adding if they rejoin
                _processedUserIds.Remove(data.UserId);

                // Add system message with nickname
                Messages.Add(new ChatMessage
                {
                    SenderId = "system",
                    SenderNickname = "System",
                    Content = $"👋 {data.Nickname} left the chat",
                    SentAt = DateTimeOffset.UtcNow,
                    IsCurrentUser = false
                });

                // Remove from 3D scene
                if (_is3DEnabled)
                {
                    await _webViewService.RemoveAvatarAsync(data.UserId);
                }

                // Check if selected avatar was removed
                if (SelectedAvatar?.UserId.ToString() == data.UserId)
                {
                    // Select fallback: self if exists, otherwise nearest
                    await SelectFallbackAvatarAsync();
                }

                // Sync avatars from 3D scene and update selection
                await SyncAvatarsAndUpdateSelection();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling user left");
        }
    }

    private void OnUserPlayAnimation(object? sender, (string userId, string animationType) data)
    {
        try
        {
            _logger.LogInformation("User {UserId} played animation {AnimationType}",
                data.userId, data.animationType);

            // Skip if 3D is disabled
            if (!Is3DEnabled || !_webViewService.IsSceneReady)
            {
                _logger.LogDebug("3D scene not ready, skipping animation");
                return;
            }

            // Always play animation for the target user (including self)
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    var success = await _webViewService.PlayAnimationAsync(data.userId, data.animationType, false);
                    if (success)
                    {
                        _logger.LogDebug("✅ Played animation {AnimationType} for user {UserId}",
                            data.animationType, data.userId);
                    }
                    else
                    {
                        _logger.LogWarning("❌ Failed to play animation {AnimationType} for user {UserId}",
                            data.animationType, data.userId);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error playing animation for user {UserId}", data.userId);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling user play animation");
        }
    }

    /// <summary>
    /// Selects a fallback avatar when current selection is removed
    /// </summary>
    private async Task SelectFallbackAvatarAsync()
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            // Try to select self first
            var selfAvatar = OnlineAvatars.FirstOrDefault(a => a.IsCurrentUser);
            if (selfAvatar != null)
            {
                SelectedAvatar = selfAvatar;
                // Center camera on self
                if (_is3DEnabled && _webViewService.IsSceneReady)
                {
                    await _webViewService.EvaluateJavaScriptAsync($"hubbly3d.centerOnAvatar('{selfAvatar.UserId}')", _cts.Token);
                }
                return;
            }

            // If no self, select first available
            if (OnlineAvatars.Any())
            {
                SelectedAvatar = OnlineAvatars.First();
                if (_is3DEnabled && _webViewService.IsSceneReady)
                {
                    await _webViewService.EvaluateJavaScriptAsync($"hubbly3d.centerOnAvatar('{SelectedAvatar.UserId}')", _cts.Token);
                }
                return;
            }

            // No avatars left
            SelectedAvatar = null;
            IsControlPanelVisible = false;
        });
    }

    private void OnUserTyping(object sender, string userId)
    {
        try
        {
            if (userId == _userId) return; // Ignore self

            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsTypingVisible = true;

                // Hide after 3 seconds
                Task.Delay(3000).ContinueWith(_ =>
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        IsTypingVisible = false;
                    });
                });
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling user typing");
        }
    }

    private void OnAssignedToRoom(object sender, RoomAssignmentData data)
    {
        try
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                RoomName = data.RoomName;
                UsersInRoom = data.UsersInRoom;
                MaxUsers = data.MaxUsers;
                RoomStatus = $"{data.UsersInRoom}/{data.MaxUsers}";

                _logger.LogInformation("Assigned to room: {RoomName}", data.RoomName);
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling room assignment");
        }
    }

    private async void OnInitialPresenceReceived(object sender, IEnumerable<UserJoinedData> existingUsers)
    {
        try
        {
            var users = existingUsers?.ToList() ?? new();
            if (!users.Any())
            {
                _logger.LogInformation("No existing users to add");
                return;
            }

            _logger.LogInformation("Adding {Count} existing users to 3D scene", users.Count);

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                foreach (var user in users)
                {
                    string gender = "male";
                    try
                    {
                        if (!string.IsNullOrEmpty(user.AvatarConfigJson))
                        {
                            var config = AvatarConfigDto.FromJson(user.AvatarConfigJson);
                            gender = config.Gender;
                        }
                    }
                    catch { gender = "male"; }

                    await Ensure3DAvatarPresence(user.UserId, user.Nickname, gender);
                    await Task.Delay(50, _cts.Token); // Small pause between additions
                }

                IsInitialPresenceLoaded = true;
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling initial presence");
        }
    }

    private void OnErrorReceived(object sender, string error)
    {
        try
        {
            _logger.LogError("SignalR error: {Error}", error);

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                ConnectionError = error;
                HasConnectionError = true;
                await Shell.Current.DisplayAlert("Chat Error", error, "OK");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling error message");
        }
    }

    private void OnAuthenticationFailed(object sender, EventArgs e)
    {
        try
        {
            _logger.LogWarning("Authentication failed");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                IsConnected = false;
                await Shell.Current.DisplayAlert(
                    "Session Expired",
                    "Your session has expired. Please login again.",
                    "OK");
                await _navigationService.NavigateToAsync("//welcome");
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling authentication failure");
        }
    }

    private void OnConnectionStateChanged(object sender, SignalRService.ConnectionStateChangedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Connection state changed: {State}", e.State);

            MainThread.BeginInvokeOnMainThread(() =>
            {
                IsConnected = e.State == HubConnectionState.Connected;

                if (!string.IsNullOrEmpty(e.Error))
                {
                    ConnectionError = e.Error;
                    HasConnectionError = true;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling connection state change");
        }
    }

    #endregion

    #region WebView Event Handlers

    private async void OnAvatarClicked(object sender, string userId)
    {
        try
        {
            userId = userId?.Trim().Trim('"').Trim('\'') ?? "";
            var normalizedUserId = _userId?.Trim().Trim('"').Trim('\'') ?? "";

            _logger.LogInformation("Avatar clicked - UserId: {UserId}, CurrentUser: {CurrentUserId}",
                userId, normalizedUserId);

            // Check if it's own avatar
            if (Guid.TryParse(userId, out var clickedGuid) &&
                Guid.TryParse(normalizedUserId, out var currentGuid) &&
                clickedGuid == currentGuid)
            {
                await ShowAvatarActionSheet();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling avatar click");
        }
    }

    private async void OnSceneReady(object sender, string message)
    {
        _logger.LogInformation("3D Scene ready");
        Is3DEnabled = true;
        UseFallbackAvatars = false;
        
        _logger.LogInformation("OnSceneReady: Starting avatar sync after 1s delay");
        
        // Sync avatars from 3D scene and update selection
        _ = Task.Run(async () =>
        {
            await Task.Delay(1000); // Wait for initial avatars to load
            _logger.LogInformation("OnSceneReady: Calling SyncAvatarsAndUpdateSelection");
            await SyncAvatarsAndUpdateSelection();
        });
    }

    private void OnSceneError(object sender, string error)
    {
        _logger.LogError("3D Scene error: {Error}", error);
        Disable3D();
    }

    #endregion

    #region Private methods

    private void HideKeyboard()
    {
#if ANDROID
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var activity = Platform.CurrentActivity;
            var inputMethodManager = activity.GetSystemService(Android.Content.Context.InputMethodService)
                as Android.Views.InputMethods.InputMethodManager;
            var currentFocus = activity.CurrentFocus;
            if (currentFocus != null)
            {
                inputMethodManager?.HideSoftInputFromWindow(currentFocus.WindowToken, 0);
                currentFocus.ClearFocus();
            }
        });
#endif
    }

    private async Task AddSelfTo3DScene()
    {
        if (!Is3DEnabled) return;

        try
        {
            _logger.LogInformation("Adding self to 3D scene");

            // Wait for scene readiness with timeout (30 seconds for mobile)
            var sceneReady = await WaitForSceneWithTimeout(30000);

            if (!sceneReady)
            {
                _logger.LogWarning("3D scene timeout");
                Disable3D();
                return;
            }

            var userId = await _tokenManager.GetAsync("user_id");
            var nickname = await _tokenManager.GetEncryptedAsync("nickname") ?? "Guest";
            var avatarConfigJson = await _tokenManager.GetEncryptedAsync("avatar_config") ?? "{}";

            if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(nickname))
            {
                _logger.LogWarning("No user data found for self");
                return;
            }

            var avatarConfig = AvatarConfigDto.FromJson(avatarConfigJson);
            var gender = avatarConfig?.Gender ?? "male";

            var success = await _webViewService.AddAvatarAsync(userId, nickname, gender, true);

            if (success)
            {
                _logger.LogInformation("✅ Self added to 3D scene: {Nickname}", nickname);
                
                // Auto-select current user's avatar and center camera
                _logger.LogInformation("Auto-selecting self avatar: {UserId}", userId);
                var selectSuccess = await _webViewService.SelectAvatarAsync(userId);
                if (selectSuccess)
                {
                    _logger.LogInformation("✅ Camera centered on self");
                    // Wait for camera animation to complete
                    await Task.Delay(300, _cts.Token);
                    // Update selected avatar from 3D scene
                    await SyncAvatarsAndUpdateSelection();
                }
                else
                {
                    _logger.LogWarning("❌ Failed to center camera on self");
                }
            }
            else
            {
                _logger.LogWarning("❌ Failed to add self to 3D scene");
                Disable3D();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Add self to 3D error");
            Disable3D();
        }
    }

    private async Task<bool> WaitForSceneWithTimeout(int timeoutMs)
    {
        var startTime = DateTime.Now;
        var checkInterval = 500; // Check every 500ms

        while (!_webViewService.IsSceneReady)
        {
            if ((DateTime.Now - startTime).TotalMilliseconds > timeoutMs)
            {
                _logger.LogWarning("Scene ready timeout after {TimeoutMs}ms", timeoutMs);
                return false;
            }

            // Log progress every 3 seconds
            var elapsed = (DateTime.Now - startTime).TotalMilliseconds;
            if (elapsed > 0 && (elapsed % 3000) < 100)
            {
                _logger.LogDebug("Waiting for scene... ({Elapsed}ms elapsed)", elapsed);
            }

            await Task.Delay(checkInterval, _cts.Token);
        }

        return true;
    }

    private async Task Ensure3DAvatarPresence(string userId, string nickname, string gender)
    {
        if (!Is3DEnabled) return;

        await _avatarLock.WaitAsync(_cts.Token);
        try
        {
            if (_processedUserIds.Contains(userId))
            {
                _logger.LogDebug("Avatar {Nickname} already processed", nickname);
                return;
            }

            _processedUserIds.Add(userId);

            // Add to UI list
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (!OnlineAvatars.Any(a => a.UserId.ToString() == userId))
                {
                    OnlineAvatars.Add(new AvatarPresence
                    {
                        UserId = Guid.Parse(userId),
                        Nickname = nickname,
                        AvatarConfig = AvatarConfigDto.FromJson($"{{\"gender\":\"{gender}\"}}"),
                        JoinedAt = DateTime.UtcNow,
                        IsCurrentUser = false
                    });
                }
            });

            // Add to 3D scene
            var success = await _webViewService.AddAvatarAsync(userId, nickname, gender);

            if (success)
            {
                _logger.LogDebug("✅ Avatar {Nickname} added to 3D scene", nickname);
            }
            else
            {
                _logger.LogWarning("❌ Failed to add {Nickname} to 3D scene", nickname);
            }
        }
        finally
        {
            _avatarLock.Release();
        }
    }

    private async Task ShowAvatarActionSheet()
    {
        try
        {
            var page = Application.Current?.MainPage;
            if (page == null)
            {
                _logger.LogError("Cannot show action sheet - MainPage is null");
                return;
            }

            var action = await page.DisplayActionSheet(
                "Avatar Action",
                "Cancel",
                null,
                "👏 Clap",
                "👋 Wave");

            _logger.LogDebug("Action selected: {Action}", action);

            switch (action)
            {
                case "👏 Clap":
                    await SendAnimationCommand.ExecuteAsync("clap");
                    break;
                case "👋 Wave":
                    await SendAnimationCommand.ExecuteAsync("wave");
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing action sheet");
        }
    }

    private async void UpdatePresenceInternal()
    {
        // TODO: Implement presence update
    }

    public void Disable3D()
    {
        if (_is3DEnabled)
        {
            Is3DEnabled = false;
            UseFallbackAvatars = true;
            _logger.LogWarning("3D disabled, switching to fallback UI");

            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Shell.Current.DisplayAlert(
                    "3D Mode",
                    "Switching to simple avatars mode for better performance",
                    "OK");
            });
        }
    }

    private async void SendTypingIndicatorInternal()
    {
        if (!IsConnected) return;

        try
        {
            await _signalRService.SendTypingIndicatorAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending typing indicator");
        }
    }

    #endregion

    #region IQueryAttributable

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            if (query.TryGetValue("roomId", out var roomId))
            {
                _logger.LogInformation("Opening room: {RoomId}", roomId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying query attributes");
        }
    }

    #endregion

    #region Lifecycle

    public async Task OnAppearing()
    {
        _logger.LogDebug("ChatRoomPage OnAppearing");

        try
        {
            // Сбрасываем состояние подключения для принудительного переподключения
            IsConnected = false;
            IsBusy = false;
            ConnectionError = string.Empty;
            HasConnectionError = false;

            // Load user data first
            await LoadUserDataAsync();

            // Check server health
            var isAvailable = await _authService.CheckServerHealthAsync();

            if (!isAvailable)
            {
                ConnectionError = "Chat server is not responding";
                HasConnectionError = true;
                return;
            }

            // Всегда пытаемся подключиться при появлении страницы
            await ConnectToChat();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OnAppearing");
            ConnectionError = "Failed to initialize chat. Please restart the app.";
            HasConnectionError = true;
            throw; // Re-throw so global exception handler can catch it
        }
    }

    public async Task OnDisappearing()
    {
        _logger.LogDebug("ChatRoomPage disappearing");
        
        if (IsConnected && !_isLeaving)
        {
            await DisconnectFromChat();
        }
    }

    partial void OnMessageTextChanged(string value)
    {
        if (!string.IsNullOrEmpty(value) && IsConnected)
        {
            if ((DateTime.Now - _lastTypingTime).TotalSeconds > 2)
            {
                _lastTypingTime = DateTime.Now;
                
                if (SendTypingIndicatorCommand.CanExecute(null))
                {
                    SendTypingIndicatorCommand.Execute(null);
                }
            }
        }
    }

    #endregion

    #region IDisposable

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;

        _logger.LogInformation("ChatRoomViewModel disposing async...");

        // Cancel and dispose CTS
        _cts.Cancel();
        _cts.Dispose();

        // Clear 3D avatars first
        if (_is3DEnabled)
        {
            try
            {
                await _webViewService.ClearAvatarsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing avatars during dispose");
            }
        }

        // Disconnect from chat
        try
        {
            await DisconnectFromChat();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disconnecting from chat during dispose");
        }

        // Dispose sync resources
        _connectionLock.Dispose();
        _avatarLock.Dispose();
        _messageLock.Dispose();

        _typingDebouncer.Dispose();
        _presenceDebouncer.Dispose();

        UnsubscribeSignalREvents();

        _processedUserIds.Clear();
        Messages.Clear();
        OnlineAvatars.Clear();

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("ChatRoomViewModel disposed async");
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("ChatRoomViewModel disposing sync...");

        // Call async dispose synchronously for compatibility
        DisposeAsync().AsTask().GetAwaiter().GetResult();
    }

    #endregion

    #region DTOs for 3D Scene

    public class AvatarAtCenterDto
    {
        [JsonPropertyName("userId")]
        public string UserId { get; set; } = string.Empty;
        
        [JsonPropertyName("nickname")]
        public string Nickname { get; set; } = string.Empty;
        
        [JsonPropertyName("gender")]
        public string Gender { get; set; } = "male";
        
        [JsonPropertyName("slotIndex")]
        public int SlotIndex { get; set; }
        
        [JsonPropertyName("xPosition")]
        public float XPosition { get; set; }
        
        [JsonPropertyName("isLoaded")]
        public bool IsLoaded { get; set; }
    }

    public class AvatarInfoDto : AvatarAtCenterDto
    {
        // Same structure, used for avatar list
    }

    #endregion

    #region Navigation and Control Panel

    /// <summary>
    /// Updates the selected avatar based on 3D scene state
    /// </summary>
    private async Task UpdateSelectedAvatarFrom3DScene()
    {
        _logger.LogInformation("UpdateSelectedAvatarFrom3DScene called - Is3DEnabled: {Is3DEnabled}, SceneReady: {SceneReady}, OnlineAvatars count: {Count}",
            Is3DEnabled, _webViewService?.IsSceneReady, OnlineAvatars.Count);
        
        if (!Is3DEnabled || !_webViewService.IsSceneReady)
        {
            _logger.LogWarning("UpdateSelectedAvatarFrom3DScene: Skipping - 3D not enabled or scene not ready");
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                SelectedAvatar = null;
                IsControlPanelVisible = false;
                CanNavigateLeft = false;
                CanNavigateRight = false;
            });
            return;
        }

        try
        {
            _logger.LogInformation("UpdateSelectedAvatarFrom3DScene: Calling hubbly3d.getAvatarAtCenter()");
            var avatarJson = await _webViewService.EvaluateJavaScriptAsync("hubbly3d.getAvatarAtCenter()", _cts.Token);
            _logger.LogInformation("UpdateSelectedAvatarFrom3DScene: getAvatarAtCenter returned: {Json}", avatarJson);
            
            if (string.IsNullOrEmpty(avatarJson) || avatarJson == "null")
            {
                _logger.LogWarning("UpdateSelectedAvatarFrom3DScene: getAvatarAtCenter returned null or empty");
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    SelectedAvatar = null;
                    IsControlPanelVisible = false;
                    CanNavigateLeft = false;
                    CanNavigateRight = false;
                });
                return;
            }

            AvatarAtCenterDto? avatarData = null;
            try
            {
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                avatarData = System.Text.Json.JsonSerializer.Deserialize<AvatarAtCenterDto>(avatarJson, options);
                if (avatarData == null)
                {
                    _logger.LogWarning("UpdateSelectedAvatarFrom3DScene: Deserialization returned null");
                    return;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "UpdateSelectedAvatarFrom3DScene: Failed to deserialize avatarAtCenter JSON: {Json}", avatarJson);
                return;
            }

            _logger.LogInformation("UpdateSelectedAvatarFrom3DScene: Got avatar {Nickname} at slot {SlotIndex}, userId: {UserId}",
                avatarData.Nickname, avatarData.SlotIndex, avatarData.UserId);

            // Find in OnlineAvatars collection (parse userId to GUID for comparison)
            if (!Guid.TryParse(avatarData.UserId, out var avatarGuid))
            {
                _logger.LogWarning("UpdateSelectedAvatarFrom3DScene: Invalid GUID format for userId: {UserId}", avatarData.UserId);
                return;
            }
            
            var avatarPresence = OnlineAvatars.FirstOrDefault(a => a.UserId == avatarGuid);
            _logger.LogInformation("UpdateSelectedAvatarFrom3DScene: Found in OnlineAvatars: {Found}", avatarPresence != null);

            // FALLBACK: If avatar not found in OnlineAvatars, try to select current user or first available
            if (avatarPresence == null && OnlineAvatars.Any())
            {
                _logger.LogWarning("UpdateSelectedAvatarFrom3DScene: Avatar not found in OnlineAvatars, using fallback");
                avatarPresence = OnlineAvatars.FirstOrDefault(a => a.IsCurrentUser) ?? OnlineAvatars.First();
                _logger.LogInformation("UpdateSelectedAvatarFrom3DScene: Fallback selected: {Nickname}", avatarPresence.Nickname);
            }

            // Get all avatars from 3D scene
            _logger.LogInformation("UpdateSelectedAvatarFrom3DScene: Calling hubbly3d.getAvatars()");
            var allAvatarsJson = await _webViewService.EvaluateJavaScriptAsync("hubbly3d.getAvatars()", _cts.Token);
            _logger.LogInformation("UpdateSelectedAvatarFrom3DScene: getAvatars returned: {Json}", allAvatarsJson);
            
            List<AvatarAtCenterDto>? allAvatarsData = null;
            if (!string.IsNullOrEmpty(allAvatarsJson) && allAvatarsJson != "null")
            {
                try
                {
                    var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    allAvatarsData = System.Text.Json.JsonSerializer.Deserialize<List<AvatarAtCenterDto>>(allAvatarsJson, options);
                    if (allAvatarsData != null)
                    {
                        _logger.LogInformation("UpdateSelectedAvatarFrom3DScene: Deserialized {Count} avatars from getAvatars()", allAvatarsData.Count);
                    }
                }
                catch (JsonException ex)
                {
                    _logger.LogError(ex, "UpdateSelectedAvatarFrom3DScene: Failed to deserialize getAvatars JSON: {Json}", allAvatarsJson);
                }
            }

            // Update all properties on main thread
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                if (avatarPresence != null)
                {
                    SelectedAvatar = avatarPresence;
                    IsControlPanelVisible = true;
                    _logger.LogInformation("UpdateSelectedAvatarFrom3DScene: Set SelectedAvatar and IsControlPanelVisible=true");
                }
                else
                {
                    SelectedAvatar = null;
                    IsControlPanelVisible = false;
                    _logger.LogWarning("UpdateSelectedAvatarFrom3DScene: avatarPresence is null, hiding panel");
                }

                // Simplified navigation logic based on SelectedAvatar slot position
                if (SelectedAvatar != null && allAvatarsData != null && allAvatarsData.Count > 1)
                {
                    var currentSlot = allAvatarsData.FirstOrDefault(a => a.UserId == SelectedAvatar.UserId.ToString())?.SlotIndex ?? 0;
                    CanNavigateLeft = allAvatarsData.Any(a => a.SlotIndex < currentSlot);
                    CanNavigateRight = allAvatarsData.Any(a => a.SlotIndex > currentSlot);
                    _logger.LogInformation("UpdateSelectedAvatarFrom3DScene: Navigation - Left: {Left}, Right: {Right}, Slot: {Slot}",
                        CanNavigateLeft, CanNavigateRight, currentSlot);
                }
                else
                {
                    CanNavigateLeft = false;
                    CanNavigateRight = false;
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating selected avatar from 3D scene");
        }
    }

    /// <summary>
    /// Synchronizes avatars list with 3D scene and updates selection atomically
    /// </summary>
    private async Task SyncAvatarsAndUpdateSelection()
    {
        await _syncLock.WaitAsync(_cts.Token);
        try
        {
            _logger.LogInformation("SyncAvatarsAndUpdateSelection started");
            
            await SyncAvatarsFrom3DScene();
            
            // Ensure we have a selected avatar after sync
            if (SelectedAvatar == null && OnlineAvatars.Any())
            {
                var fallback = OnlineAvatars.FirstOrDefault(a => a.IsCurrentUser) ?? OnlineAvatars.First();
                _logger.LogWarning("SyncAvatarsAndUpdateSelection: No selected avatar, fallback to {Nickname}", fallback.Nickname);
                
                // Center camera on fallback avatar
                var userId = fallback.UserId.ToString();
                var selectSuccess = await _webViewService.SelectAvatarAsync(userId);
                if (selectSuccess)
                {
                    await Task.Delay(200, _cts.Token);
                }
            }
            
            await UpdateSelectedAvatarFrom3DScene();
            _logger.LogInformation("SyncAvatarsAndUpdateSelection completed");
        }
        finally
        {
            _syncLock.Release();
        }
    }

    /// <summary>
    /// Synchronizes avatars list with 3D scene (called on SignalR events)
    /// </summary>
    private async Task SyncAvatarsFrom3DScene()
    {
        _logger.LogInformation("SyncAvatarsFrom3DScene called - Is3DEnabled: {Is3DEnabled}, SceneReady: {SceneReady}, OnlineAvatars before: {Count}",
            Is3DEnabled, _webViewService?.IsSceneReady, OnlineAvatars.Count);
        
        if (!Is3DEnabled || !_webViewService.IsSceneReady)
        {
            _logger.LogWarning("SyncAvatarsFrom3DScene: Skipping - 3D not enabled or scene not ready");
            return;
        }

        try
        {
            _logger.LogInformation("SyncAvatarsFrom3DScene: Calling hubbly3d.getAvatars()");
            var avatarsJson = await _webViewService.EvaluateJavaScriptAsync("hubbly3d.getAvatars()", _cts.Token);
            _logger.LogInformation("SyncAvatarsFrom3DScene: getAvatars returned: {Json}", avatarsJson);
            
            if (string.IsNullOrEmpty(avatarsJson) || avatarsJson == "null")
            {
                _logger.LogWarning("SyncAvatarsFrom3DScene: getAvatars returned null or empty");
                return;
            }

            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var avatars = System.Text.Json.JsonSerializer.Deserialize<List<AvatarInfoDto>>(avatarsJson, options);
            if (avatars == null)
            {
                _logger.LogWarning("SyncAvatarsFrom3DScene: Deserialization returned null");
                return;
            }

            _logger.LogInformation("SyncAvatarsFrom3DScene: Deserialized {Count} avatars from 3D scene", avatars.Count);

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                // Update OnlineAvatars collection based on 3D scene
                foreach (var avatarInfo in avatars)
                {
                    // Try to parse UserId as GUID
                    if (!Guid.TryParse(avatarInfo.UserId, out var userIdGuid))
                    {
                        _logger.LogWarning("SyncAvatarsFrom3DScene: Invalid GUID format for userId '{UserId}', skipping avatar {Nickname}",
                            avatarInfo.UserId, avatarInfo.Nickname);
                        continue;
                    }

                    var existing = OnlineAvatars.FirstOrDefault(a => a.UserId == userIdGuid);
                    if (existing == null)
                    {
                        OnlineAvatars.Add(new AvatarPresence
                        {
                            UserId = userIdGuid,
                            Nickname = avatarInfo.Nickname,
                            AvatarConfig = new AvatarConfigDto { Gender = avatarInfo.Gender },
                            JoinedAt = DateTime.UtcNow,
                            IsCurrentUser = avatarInfo.UserId == _userId
                        });
                        _logger.LogInformation("SyncAvatarsFrom3DScene: Added avatar {Nickname} ({UserId})", avatarInfo.Nickname, userIdGuid);
                    }
                }

                // Remove avatars that are no longer in 3D scene
                var toRemove = OnlineAvatars.Where(a => !avatars.Any(v => Guid.TryParse(v.UserId, out var vid) && vid == a.UserId)).ToList();
                foreach (var removed in toRemove)
                {
                    _logger.LogInformation("SyncAvatarsFrom3DScene: Removing avatar {Nickname} ({UserId})", removed.Nickname, removed.UserId);
                    OnlineAvatars.Remove(removed);
                }
            });

            _logger.LogInformation("SyncAvatarsFrom3DScene: OnlineAvatars count after sync: {Count}", OnlineAvatars.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing avatars from 3D scene");
            throw; // Re-throw for caller
        }
    }

    [RelayCommand]
    private async Task NavigateLeft()
    {
        _logger.LogInformation("NavigateLeft command executed - CanNavigateLeft: {CanNavigateLeft}, IsHoldNavigationActive: {IsHoldActive}",
            CanNavigateLeft, IsHoldNavigationActive);
        
        if (!CanNavigateLeft)
        {
            _logger.LogWarning("NavigateLeft: Cannot navigate left (CanNavigateLeft=false)");
            return;
        }

        IsHoldNavigationActive = true;
        try
        {
            _logger.LogInformation("NavigateLeft: Calling hubbly3d.startHoldNavigation('prev')");
            var result = await _webViewService.EvaluateJavaScriptAsync("hubbly3d.startHoldNavigation('prev')", _cts.Token);
            _logger.LogInformation("NavigateLeft: startHoldNavigation returned: {Result}", result);
            
            // After a short delay, stop the hold navigation (single step)
            await Task.Delay(500);
            _logger.LogInformation("NavigateLeft: Calling hubbly3d.stopHoldNavigation()");
            _ = _webViewService.EvaluateJavaScriptAsync("hubbly3d.stopHoldNavigation()", _cts.Token);
            
            // Sync and update selection after navigation
            await Task.Delay(600); // Wait for animation
            _logger.LogInformation("NavigateLeft: Syncing avatars and updating selection");
            await SyncAvatarsAndUpdateSelection();
        }
        finally
        {
            IsHoldNavigationActive = false;
        }
    }

    [RelayCommand]
    private async Task NavigateRight()
    {
        _logger.LogInformation("NavigateRight command executed - CanNavigateRight: {CanNavigateRight}, IsHoldNavigationActive: {IsHoldActive}",
            CanNavigateRight, IsHoldNavigationActive);
        
        if (!CanNavigateRight)
        {
            _logger.LogWarning("NavigateRight: Cannot navigate right (CanNavigateRight=false)");
            return;
        }

        IsHoldNavigationActive = true;
        try
        {
            _logger.LogInformation("NavigateRight: Calling hubbly3d.startHoldNavigation('next')");
            var result = await _webViewService.EvaluateJavaScriptAsync("hubbly3d.startHoldNavigation('next')", _cts.Token);
            _logger.LogInformation("NavigateRight: startHoldNavigation returned: {Result}", result);
            
            // After a short delay, stop the hold navigation (single step)
            await Task.Delay(500);
            _logger.LogInformation("NavigateRight: Calling hubbly3d.stopHoldNavigation()");
            _ = _webViewService.EvaluateJavaScriptAsync("hubbly3d.stopHoldNavigation()", _cts.Token);
            
            // Sync and update selection after navigation
            await Task.Delay(600); // Wait for animation
            _logger.LogInformation("NavigateRight: Syncing avatars and updating selection");
            await SyncAvatarsAndUpdateSelection();
        }
        finally
        {
            IsHoldNavigationActive = false;
        }
    }

    [RelayCommand]
    private async Task ShowContextMenu()
    {
        if (SelectedAvatar == null) return;

        try
        {
            var page = Application.Current?.MainPage;
            if (page == null) return;

            var isCurrentUser = SelectedAvatar.UserId.ToString() == _userId;
            var actions = new List<string>();

            if (isCurrentUser)
            {
                actions.Add("Профиль (заглушка)");
                actions.Add("Действие");
            }
            else
            {
                actions.Add("Профиль (заглушка)");
            }

            var action = await page.DisplayActionSheet(
                $"Действия: {SelectedAvatar.Nickname}",
                "Отмена",
                null,
                actions.ToArray());

            if (action == "Профиль (заглушка)")
            {
                // TODO: Implement profile page
                await page.DisplayAlert("Профиль", "Страница профиля пока не реализована", "OK");
            }
            else if (action == "Действие")
            {
                await ShowAnimationMenuAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing context menu");
        }
    }

    private async Task ShowAnimationMenuAsync()
    {
        if (SelectedAvatar == null) return;

        try
        {
            var page = Application.Current?.MainPage;
            if (page == null) return;

            var actions = new[] { "Плопать", "Махать" };
            
            var action = await page.DisplayActionSheet(
                "Анимации",
                "Отмена",
                null,
                actions);

            if (!string.IsNullOrEmpty(action))
            {
                var animationName = action == "Плопать" ? "clap" : "wave";
                var targetUserId = SelectedAvatar.UserId.ToString();
                var isSelf = targetUserId == _userId;

                if (isSelf)
                {
                    // For self: send with targetUserId = null (server will use caller's userId)
                    _logger.LogInformation("Sending animation for self: {AnimationType}", animationName);
                    await _signalRService.SendAnimationAsync(animationName, null);
                }
                else
                {
                    // For another user: send with their userId as target
                    _logger.LogInformation("Sending animation for other user: {UserId}", targetUserId);
                    await _signalRService.SendAnimationAsync(animationName, targetUserId);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error showing animation menu");
        }
    }

    #endregion

    #region Public Methods for View

    /// <summary>
    /// Refreshes the selected avatar from 3D scene (called from View after navigation)
    /// </summary>
    public async Task RefreshSelectedAvatarAsync()
    {
        await SyncAvatarsAndUpdateSelection();
    }

    #endregion

    #region Helper Classes

    private class Debouncer : IDisposable
    {
        private readonly TimeSpan _delay;
        private readonly Action _action;
        private readonly SemaphoreSlim _lock = new(1, 1);
        private CancellationTokenSource _cts;
        private bool _disposed;

        public Debouncer(TimeSpan delay, Action action)
        {
            _delay = delay;
            _action = action;
            _cts = new CancellationTokenSource();
        }

        public void Invoke()
        {
            if (_disposed) return;

            _lock.Wait();
            try
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = new CancellationTokenSource();

                Task.Delay(_delay, _cts.Token).ContinueWith(t =>
                {
                    if (!t.IsCanceled && !_disposed)
                    {
                        _action();
                    }
                });
            }
            finally
            {
                _lock.Release();
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _cts.Cancel();
            _cts.Dispose();
            _lock.Dispose();
        }
    }

    #endregion
}