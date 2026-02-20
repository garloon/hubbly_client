using Android.OS;
using Microsoft.Extensions.Logging;
using Environment = Android.OS.Environment;

namespace Hubbly.Mobile.Services;

public interface ILogViewerService
{
    Task<string> GetCurrentLogPath();
    Task<string> ReadLogsAsync();
    Task ShareLogsAsync();
    Task ClearOldLogsAsync(int keepDays = 3);
}

public class LogViewerService : ILogViewerService
{
    private readonly ILogger<LogViewerService> _logger;

    public LogViewerService(ILogger<LogViewerService> logger)
    {
        _logger = logger;
    }

    public async Task<string> GetCurrentLogPath()
    {
        try
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                var downloads = Environment.GetExternalStoragePublicDirectory(
                    Environment.DirectoryDownloads).AbsolutePath;

                _logger.LogDebug("Downloads path: {Path}", downloads);

                var today = DateTime.Now.ToString("yyyyMMdd");
                var logPath = Path.Combine(downloads, $"hubbly_debug_{today}.log");

                _logger.LogDebug("Looking for log: {Path}", logPath);
                _logger.LogDebug("File exists: {Exists}", File.Exists(logPath));

                // Если сегодняшнего нет, берем последний
                if (!File.Exists(logPath))
                {
                    var files = Directory.GetFiles(downloads, "hubbly_debug_*.log");
                    _logger.LogDebug("Found {Count} log files", files.Length);

                    logPath = files.OrderByDescending(f => f).FirstOrDefault() ?? logPath;
                }

                return logPath;
            }

            return Path.Combine(FileSystem.CacheDirectory, "hubbly_debug.log");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting log path");
            return null;
        }
    }

    public async Task<string> ReadLogsAsync()
    {
        try
        {
            var logPath = await GetCurrentLogPath();
            if (File.Exists(logPath))
            {
                return await File.ReadAllTextAsync(logPath);
            }
            return "Логов не найдено";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read logs");
            return $"Ошибка чтения логов: {ex.Message}";
        }
    }

    public async Task ShareLogsAsync()
    {
        try
        {
            var logPath = await GetCurrentLogPath();
            
            if (string.IsNullOrEmpty(logPath))
            {
                _logger.LogError("Log path is empty");
                await Shell.Current.DisplayAlert("Ошибка", "Путь к логам не найден", "OK");
                return;
            }

            if (!File.Exists(logPath))
            {
                _logger.LogError("Log file not found: {Path}", logPath);
                await Shell.Current.DisplayAlert("Ошибка", "Файл логов не найден", "OK");
                return;
            }

            _logger.LogInformation("Sharing log file: {Path}", logPath);

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Поделиться логами Hubbly",
                File = new ShareFile(logPath)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to share logs");
            await Shell.Current.DisplayAlert("Ошибка", $"Не удалось поделиться логами: {ex.Message}", "OK");
        }
    }

    public async Task ClearOldLogsAsync(int keepDays = 3)
    {
        try
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                var downloads = Environment.GetExternalStoragePublicDirectory(
                    Environment.DirectoryDownloads).AbsolutePath;
                var files = Directory.GetFiles(downloads, "hubbly_debug_*.log");
                var cutoff = DateTime.Now.AddDays(-keepDays);

                foreach (var file in files)
                {
                    var fileDate = File.GetCreationTime(file);
                    if (fileDate < cutoff)
                    {
                        File.Delete(file);
                        _logger.LogInformation("Deleted old log: {File}", file);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear old logs");
        }
    }
}