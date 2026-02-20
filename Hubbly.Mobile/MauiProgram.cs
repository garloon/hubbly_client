using CommunityToolkit.Maui;
using Hubbly.Mobile.Converters;
using Hubbly.Mobile.Services;
using Hubbly.Mobile.ViewModels;
using Hubbly.Mobile.Views;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
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

        // Настройка платформозависимых обработчиков
        ConfigurePlatformHandlers(builder);

        // Настройка логирования в файл
        ConfigureFileLogging(builder);
        
        // Регистрация сервисов
        RegisterServices(builder.Services);

        // Регистрация ViewModels
        RegisterViewModels(builder.Services);

        // Регистрация страниц
        RegisterPages(builder.Services);

        // Настройка логирования
        ConfigureLogging(builder);

        // OpenTelemetry Configuration (consistent with backend)
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource
                .AddService(serviceName: "Hubbly.Mobile", serviceVersion: "1.0.0"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddHttpClientInstrumentation()
                    .AddConsoleExporter();
            });

        var app = builder.Build();
        ServiceProvider = app.Services;

        // Инициализация сервисов при старте
        InitializeServices(ServiceProvider);

        return app;
    }

    #region Конфигурация платформы

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

    #region Регистрация сервисов

    private static void RegisterServices(IServiceCollection services)
    {
        // Инфраструктурные сервисы (Singleton)
        services.AddSingleton<DeviceIdService>();
        services.AddSingleton<EncryptionService>(sp =>
        {
            var deviceIdService = sp.GetRequiredService<DeviceIdService>();
            var logger = sp.GetRequiredService<ILogger<EncryptionService>>();
            // Get device ID synchronously during startup
            var deviceId = deviceIdService.GetPersistentDeviceId();
            return new EncryptionService(logger, deviceId);
        });
        services.AddSingleton<TokenManager>();
        services.AddSingleton<WebViewService>();
        services.AddSingleton<INavigationService, SimpleNavigationService>();

        services.AddSingleton<ILogViewerService, LogViewerService>();

        // HTTP клиент (Singleton)
        services.AddSingleton<HttpClient>(sp =>
        {
            var deviceIdService = sp.GetRequiredService<DeviceIdService>();
            var handler = new HttpClientHandler();

            // ВАЖНО: Не отключаем проверку сертификатов!
            // Для разработки с самоподписанными сертификатами используйте доверенные сертификаты

            var client = new HttpClient(handler);
            
            // Читаем server URL из Preferences (может быть изменен в настройках)
            var serverUrl = Preferences.Get("server_url", "http://89.169.46.33:5000");
            client.BaseAddress = new Uri(serverUrl);
            
            client.DefaultRequestHeaders.Accept.Add(new("application/json"));
            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                $"HubblyMobile/{AppInfo.Current.VersionString} ({DeviceInfo.Current.Platform}; {DeviceInfo.Current.VersionString})");

            return client;
        });

        // Бизнес-сервисы (Singleton)
        services.AddSingleton<AuthService>();
        services.AddSingleton<SignalRService>(); // Зависит от TokenManager и AuthService

        // Фоновые сервисы (Hosted)
        services.AddHostedService<ConnectionMonitorService>();
    }

    private static void RegisterViewModels(IServiceCollection services)
    {
        services.AddTransient<WelcomeViewModel>();
        services.AddTransient<AvatarSelectionViewModel>();
        services.AddTransient<ChatRoomViewModel>();
    }

    private static void RegisterPages(IServiceCollection services)
    {
        services.AddTransient<WelcomePage>();
        services.AddTransient<AvatarSelectionPage>();
        services.AddTransient<ChatRoomPage>();
    }
    
    #endregion

    #region Настройка логирования

    private static void ConfigureLogging(MauiAppBuilder builder)
    {
#if DEBUG
        builder.Logging.AddDebug();
        builder.Logging.SetMinimumLevel(LogLevel.Debug);
#else
        builder.Logging.SetMinimumLevel(LogLevel.Information);
#endif

        // Добавляем фильтры для уменьшения шума
        builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
        builder.Logging.AddFilter("System", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.AspNetCore.SignalR", LogLevel.Information);
    }

    #endregion

    #region Конфигурация логирования в файл

    private static void ConfigureFileLogging(MauiAppBuilder builder)
    {
#if DEBUG
        try
        {
            // Определяем путь для логов на Android
            string logPath;

            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                // Папка Downloads на Android
                var downloads = Android.OS.Environment.GetExternalStoragePublicDirectory(
                    Android.OS.Environment.DirectoryDownloads).AbsolutePath;
                logPath = Path.Combine(downloads, "hubbly_debug.log");

                // Создаем папку если нет
                Directory.CreateDirectory(Path.GetDirectoryName(logPath));

                // Пишем в консоль для отладки
                Console.WriteLine($"📁 Логи будут сохраняться в: {logPath}");
            }
            else
            {
                // Для других платформ - в CacheDirectory
                logPath = Path.Combine(FileSystem.CacheDirectory, "hubbly_debug.log");
            }

            // Настраиваем Serilog
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    logPath,
                    outputTemplate: "{Timestamp:HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    fileSizeLimitBytes: 10485760)
                .CreateLogger();

            // Добавляем Serilog как провайдер логов
            builder.Logging.AddSerilog(Log.Logger, dispose: true);

            // Добавляем тестовую запись при старте
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
#endif
    }

    #endregion

    #region Инициализация при старте

    private static void InitializeServices(IServiceProvider serviceProvider)
    {
        try
        {
            // Получаем сервисы для прогрева
            _ = serviceProvider.GetRequiredService<DeviceIdService>();
            _ = serviceProvider.GetRequiredService<TokenManager>();
            _ = serviceProvider.GetRequiredService<AuthService>();
            _ = serviceProvider.GetRequiredService<SignalRService>();
            _ = serviceProvider.GetRequiredService<WebViewService>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing services: {ex.Message}");
        }
    }

    #endregion
}