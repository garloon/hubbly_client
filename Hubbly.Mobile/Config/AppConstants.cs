namespace Hubbly.Mobile.Config;

/// <summary>
/// Application-wide constants to avoid magic numbers
/// </summary>
public static class AppConstants
{
    // Timeouts (seconds)
    public const int NavigationLockTimeoutSeconds = 5;
    public const int ConnectionLockTimeoutSeconds = 10;
    public const int MessageLockTimeoutSeconds = 2;
    public const int SceneLockTimeoutMilliseconds = 100;
    public const int AuthLockTimeoutSeconds = 10;
    public const int AuthTimeoutSeconds = 15;
    public const int GuestConversionTimeoutSeconds = 10;
    public const int ReconnectLockTimeoutSeconds = 1;
    
    // Delays (milliseconds)
    public const int AvatarQueueDelayMilliseconds = 150;
    public const int CameraAnimationDelayMilliseconds = 300;
    public const int HoldNavigationDelayMilliseconds = 500;
    public const int SyncAfterNavigationDelayMilliseconds = 600;
    public const int SceneReadyCheckIntervalMilliseconds = 500;
    public const int TypingIndicatorDelayMilliseconds = 1000;
    public const int PresenceUpdateDelayMilliseconds = 500;
    public const int VibrationDurationMilliseconds = 30;
    public const int InitialAvatarLoadDelayMilliseconds = 1000;
    public const int MessageQueueDelayMilliseconds = 100;
    public const int FadeAnimationDurationMilliseconds = 500;
    public const int NavigationDelayMilliseconds = 600;
    public const int RetryDelayMilliseconds = 100;
    public const int ReconnectInitialDelayMilliseconds = 1000;
    public const int HealthCheckRetryDelayMilliseconds = 500;
    public const int SceneInitialLoadDelayMilliseconds = 15000;
    public const int SceneReadyRetryIntervalMilliseconds = 5000;
    public const int MaxSceneReadyRetries = 6;
    public const int AvatarProcessingDelayMilliseconds = 150;
    public const int MaxJsMessageSize = 10000;
    
    // Timeouts (TimeSpan)
    public static readonly TimeSpan ServerHealthTimeout = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan SceneReadyTimeout = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan WebViewEvaluateTimeout = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan TokenRefreshTimeout = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan ConnectionMonitorInterval = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan ConnectionMonitorErrorDelay = TimeSpan.FromSeconds(10);
    
    // Token Expiration
    public static readonly TimeSpan AccessTokenExpiration = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan RefreshTokenExpiration = TimeSpan.FromDays(7);
    
    // SignalR
    public const int MaxReconnectAttempts = 5;
    public const int ReconnectBaseDelayMs = 2000;
    public static readonly TimeSpan SignalRCloseTimeout = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan SignalRHandshakeTimeout = TimeSpan.FromSeconds(2);
    public static readonly TimeSpan SignalRKeepAliveInterval = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan HeartbeatInterval = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan HeartbeatTimeout = TimeSpan.FromSeconds(10);
    public static readonly TimeSpan MessageQueueProcessingInterval = TimeSpan.FromSeconds(5);
    public static readonly TimeSpan MaxReconnectDelay = TimeSpan.FromSeconds(30);
    
    // Message Limits
    public const int MaxMessageLength = 500;
    public const int MaxLogScriptLength = 100;
    public const int MaxQueuedMessageAgeSeconds = 60;
    
    // Room
    public const int DefaultMaxUsersPerRoom = 50;
    
    // Logging
    public const long MaxLogFileSizeBytes = 10_485_760; // 10 MB
    
    // UI
    public const double DefaultFadeDuration = 500; // milliseconds
    
    // Chat
    public const int MaxMessagesPerRoom = 100;
}
