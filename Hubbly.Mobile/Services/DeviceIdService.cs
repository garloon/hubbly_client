using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace Hubbly.Mobile.Services;

public class DeviceIdService : IDisposable
{
    private readonly ILogger<DeviceIdService> _logger;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();
    private readonly EncryptionService _encryption;

    private string _cachedDeviceId;
    private bool _disposed;

    public DeviceIdService(ILogger<DeviceIdService> logger, EncryptionService encryption)
    {
        _logger = logger;
        _encryption = encryption ?? throw new ArgumentNullException(nameof(encryption));
    }

    #region Public Methods

    public string GetPersistentDeviceId()
    {
        ThrowIfDisposed();

        // Return cached value if available
        if (!string.IsNullOrEmpty(_cachedDeviceId))
            return _cachedDeviceId;

        // Lock for thread safety
        _lock.Wait(_cts.Token);
        try
        {
            // Double-check after acquiring lock
            if (!string.IsNullOrEmpty(_cachedDeviceId))
                return _cachedDeviceId;

            // Check saved ID (encrypted in Preferences)
            var savedIdEncrypted = Preferences.Get("persistent_device_id", string.Empty);
            if (!string.IsNullOrEmpty(savedIdEncrypted))
            {
                try
                {
                    _cachedDeviceId = _encryption.Decrypt(savedIdEncrypted);
                    _logger.LogInformation("DeviceIdService: Using saved device ID: {DeviceId}", _cachedDeviceId);
                    return _cachedDeviceId;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DeviceIdService: Failed to decrypt device ID, will generate new one");
                }
            }

            // Generate new ID
            _cachedDeviceId = GenerateSecureDeviceId();

            // Save encrypted
            var encrypted = _encryption.Encrypt(_cachedDeviceId);
            Preferences.Set("persistent_device_id", encrypted);

            _logger.LogInformation("DeviceIdService: Generated new device ID: {DeviceId}", _cachedDeviceId);

            return _cachedDeviceId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DeviceIdService: Failed to get device ID");

            // On error, generate temporary ID
            return $"TEMP_{Guid.NewGuid():N}";
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<string> GetPersistentDeviceIdAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _cts.Token);

        return await Task.Run(() => GetPersistentDeviceId(), cts.Token);
    }

    public void ResetDeviceId()
    {
        ThrowIfDisposed();

        _lock.Wait(_cts.Token);
        try
        {
            _cachedDeviceId = null;
            Preferences.Remove("persistent_device_id");

            _logger.LogInformation("DeviceIdService: Device ID reset");
        }
        finally
        {
            _lock.Release();
        }
    }
    
    public DeviceInfo GetDeviceInfo()
    {
        ThrowIfDisposed();

        return new DeviceInfo
        {
            DeviceId = GetPersistentDeviceId(),
            Platform = Microsoft.Maui.Devices.DeviceInfo.Current.Platform.ToString(),
            Model = Microsoft.Maui.Devices.DeviceInfo.Current.Model,
            Manufacturer = Microsoft.Maui.Devices.DeviceInfo.Current.Manufacturer,
            Version = Microsoft.Maui.Devices.DeviceInfo.Current.VersionString
        };
    }

    #endregion

    #region Private Methods

    private string GenerateSecureDeviceId()
    {
        try
        {
#if ANDROID
            // Try to get Android ID
            var androidId = GetAndroidId();
            if (!string.IsNullOrEmpty(androidId))
            {
                return $"DROID_{androidId}";
            }
#endif

            // Generate cryptographically secure ID
            return GenerateCryptoDeviceId();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate secure device ID, falling back to GUID");

            // Fallback to GUID
            return $"FALLBACK_{Guid.NewGuid():N}";
        }
    }

#if ANDROID
    private string GetAndroidId()
    {
        try
        {
            var context = Android.App.Application.Context;
            var androidId = Android.Provider.Settings.Secure.GetString(
                context.ContentResolver,
                Android.Provider.Settings.Secure.AndroidId);

            // Check validity (not emulator ID)
            if (!string.IsNullOrEmpty(androidId) && androidId != "9774d56d682e549c")
            {
                return androidId;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get Android ID");
        }

        return null;
    }
#endif

    private string GenerateCryptoDeviceId()
    {
        var deviceInfo = Microsoft.Maui.Devices.DeviceInfo.Current;

        // Collect unique device data
        var components = new List<string>
        {
            deviceInfo.Idiom.ToString(),
            deviceInfo.Manufacturer,
            deviceInfo.Model,
            deviceInfo.Platform.ToString(),
            deviceInfo.VersionString
        };

        // Add timestamp for uniqueness
        components.Add(DateTimeOffset.UtcNow.Ticks.ToString());

        // Create hash from components
        using var sha256 = SHA256.Create();
        var input = string.Join("|", components);
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

        // Take first 16 bytes and convert to Base64 (URL-safe)
        var deviceId = Convert.ToBase64String(hash, 0, 16)
            .Replace("/", "_")
            .Replace("+", "-")
            .TrimEnd('=');

        return $"DEV_{deviceId}";
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(DeviceIdService));
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("DeviceIdService: Disposing...");

        _cts.Cancel();
        _cts.Dispose();
        _lock.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("DeviceIdService: Disposed");
    }

    #endregion

    #region Helper Classes

    public class DeviceInfo
    {
        public string DeviceId { get; set; }
        public string Platform { get; set; }
        public string Model { get; set; }
        public string Manufacturer { get; set; }
        public string Version { get; set; }
    }

    #endregion
}