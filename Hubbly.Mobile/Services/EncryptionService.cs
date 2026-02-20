using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Services;

/// <summary>
/// Provides encryption/decryption for sensitive data stored in Preferences
/// Uses AES-256 with key derived from device ID
/// </summary>
public class EncryptionService : IDisposable
{
    private readonly ILogger<EncryptionService> _logger;
    private readonly string _deviceId;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    public EncryptionService(ILogger<EncryptionService> logger, string deviceId)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
    }

    /// <summary>
    /// Encrypts plain text value
    /// </summary>
    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        try
        {
            using var aes = Aes.Create();
            aes.Key = DeriveKey(_deviceId);
            aes.GenerateIV(); // Random IV for each encryption

            using var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream();
            using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            using (var sw = new StreamWriter(cs, Encoding.UTF8))
            {
                sw.Write(plainText);
            }

            // Combine IV + encrypted data
            var encrypted = ms.ToArray();
            var combined = new byte[aes.IV.Length + encrypted.Length];
            Buffer.BlockCopy(aes.IV, 0, combined, 0, aes.IV.Length);
            Buffer.BlockCopy(encrypted, 0, combined, aes.IV.Length, encrypted.Length);

            return Convert.ToBase64String(combined);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to encrypt value");
            throw;
        }
    }

    /// <summary>
    /// Decrypts encrypted value
    /// </summary>
    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        try
        {
            var combined = Convert.FromBase64String(encryptedText);

            using var aes = Aes.Create();
            aes.Key = DeriveKey(_deviceId);

            // Extract IV from first 16 bytes
            var iv = new byte[16];
            Buffer.BlockCopy(combined, 0, iv, 0, 16);
            aes.IV = iv;

            // Decrypt the rest
            using var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
            using var ms = new MemoryStream(combined, 16, combined.Length - 16);
            using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
            using var sr = new StreamReader(cs, Encoding.UTF8);

            return sr.ReadToEnd();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to decrypt value");
            return encryptedText; // Return as-is if decryption fails
        }
    }

    /// <summary>
    /// Derives a 256-bit key from device ID using SHA256
    /// </summary>
    private static byte[] DeriveKey(string deviceId)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(deviceId));
        // Use first 32 bytes (256 bits) for AES-256 key
        var key = new byte[32];
        Buffer.BlockCopy(hash, 0, key, 0, 32);
        return key;
    }

    public void Dispose()
    {
        if (_disposed) return;

        _lock?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}