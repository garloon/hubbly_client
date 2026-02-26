using CommunityToolkit.Maui;
using Hubbly.Mobile.Config;
using Hubbly.Mobile.Converters;
using Hubbly.Mobile.Services;
using Hubbly.Mobile.ViewModels;
using Hubbly.Mobile.Views;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Hubbly.Mobile;

public static class MauiProgram
{
    public static IServiceProvider ServiceProvider { get; private set; }

    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // Configure platform-specific handlers
        ConfigurePlatformHandlers(builder);

        // Configure logging FIRST (before registering pages that depend on ILogger<T>)
        ConfigureLogging(builder);
        
        // Configure file logging
        ConfigureFileLogging(builder);
        
        // Register services
        RegisterServices(builder.Services);

        // Register ViewModels
        RegisterViewModels(builder.Services);

        // Register pages
        RegisterPages(builder.Services);

        // OpenTelemetry Configuration (disabled for Android - causes startup issues)
        // TODO: Enable when properly configured for mobile
        // builder.Services.AddOpenTelemetry()...

        var app = builder.Build();
        ServiceProvider = app.Services;

        // Don't initialize services on startup - let them be lazy-loaded
        // This prevents long blocking operations during app initialization

        return app;
    }

    #region Platform configuration

    private static void ConfigurePlatformHandlers(MauiAppBuilder builder)
    {
#if ANDROID
        builder.ConfigureMauiHandlers(handlers =>
        {
            handlers.AddHandler(typeof(WebView), typeof(Hubbly.Mobile.Platforms.Android.CustomWebViewHandler));
        });
#endif
    }

    #endregion

    #region Service registration

    private static void RegisterServices(IServiceCollection services)
    {
        // Infrastructure services (Singleton)
        services.AddSingleton<DeviceIdService>();
        services.AddSingleton<ITokenStorage, TokenStorage>();
        services.AddSingleton<ITokenHttpService, TokenHttpService>();
        services.AddSingleton<ITokenRefresh, TokenRefresh>();
        services.AddSingleton<TokenManager>();
        services.AddSingleton<WebViewService>();
        services.AddSingleton<INavigationService, SimpleNavigationService>();
        services.AddSingleton<ILogViewerService, LogViewerService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<ILocalizationService, LocalizationService>();
        
        // Platform-specific services
#if ANDROID
        services.AddSingleton<IKeyboardService, Platforms.Android.KeyboardService>();
#else
        services.AddSingleton<IKeyboardService, NullKeyboardService>();
#endif

        // HTTP client (Singleton)
        services.AddSingleton<HttpClient>(sp =>
        {
            var handler = new HttpClientHandler();
            var client = new HttpClient(handler);
            
            // Read server URL from Preferences via ServerConfig (can be changed in settings)
            var serverUrl = ServerConfig.GetServerUrl();
            client.BaseAddress = new Uri(serverUrl);
            
            client.DefaultRequestHeaders.Accept.Add(new("application/json"));
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                $"HubblyMobile/{AppInfo.Current.VersionString} ({DeviceInfo.Current.Platform}; {DeviceInfo.Current.VersionString})");

            return client;
        });

        // Business services (Singleton)
        services.AddSingleton<AuthService>();
        services.AddSingleton<IHeartbeatService, HeartbeatService>();
        services.AddSingleton<IAvatarManagerService, AvatarManagerService>();
        services.AddSingleton<SignalRService>(); // Depends on TokenManager, AuthService, and HeartbeatService
        services.AddSingleton<RoomService>(); // REST API service for room management

        // Background services (Hosted)
        services.AddHostedService<ConnectionMonitorService>();
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddTransient<BootstrapperViewModel>();
        services.AddTransient<WelcomeViewModel>();
        services.AddTransient<AvatarSelectionViewModel>();
        services.AddTransient<ChatRoomViewModel>();
        services.AddTransient<RoomSelectionViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<AboutViewModel>();
        services.AddTransient<AppShellViewModel>();
    }

    private static void RegisterPages(IServiceCollection services)
    {
        services.AddTransient<BootstrapperPage>();
        services.AddTransient<WelcomePage>();
        services.AddTransient<AvatarSelectionPage>();
        services.AddTransient<ChatRoomPage>();
        services.AddTransient<RoomSelectionPage>();
        services.AddTransient<SettingsPage>();
        services.AddTransient<AboutPage>();
        services.AddTransient<AppShell>();
    }
    
    #endregion

    #region Logging configuration

    private static void ConfigureLogging(MauiAppBuilder builder)
    {
#if DEBUG
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#else
        builder.Logging.SetMinimumLevel(LogLevel.Information);
#endif

        // Add filters to reduce noise
        builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
        builder.Logging.AddFilter("System", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Information);
    }

    #endregion

    #region File logging configuration

    private static void ConfigureFileLogging(MauiAppBuilder builder)
    {
        try
        {
            // Determine log path for Android
            string logPath;

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // Downloads folder on Android
                var downloads = Android.OS.Environment.GetExternalStoragePublicDirectory(
                    Android.OS.Environment.DirectoryDownloads).AbsolutePath;
                logPath = Path.Combine(downloads, "hubbly.log");

                // Create folder if it doesn't exist
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));

                // Write to console for debugging
                Console.WriteLine($"📁 Logs will be saved to: {logPath}");
            }
            else
            {
                // For other platforms - in CacheDirectory
                logPath = Path.Combine(FileSystem.CacheDirectory, "hubbly.log");
            }

            // Configure Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .WriteTo.File(
                    logPath,
                    outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    fileSizeLimitBytes: 10485760)
                .CreateLogger();

            // Add Serilog as log provider
            builder.Logging.AddSerilog(Log.Logger, dispose: true);

            // Add test entry on startup
            Log.Information("=== HUBBLY APP STARTED ===");
            Log.Information("Device: {Device} {Platform}",
                DeviceInfo.Current.Model,
                DeviceInfo.Current.Platform);
            Log.Information("App version: {Version}", AppInfo.Current.VersionString);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Failed to configure file logging: {ex.Message}");
        }
    }

    #endregion

    // Services are initialized lazily when first accessed
    // This prevents blocking the UI thread during app startup
}