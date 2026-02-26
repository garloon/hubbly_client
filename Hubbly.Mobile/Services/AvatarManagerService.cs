using System.Collections.ObjectModel;
using Microsoft.Extensions.Logging;
using Hubbly.Mobile.Models;

namespace Hubbly.Mobile.Services;

public class AvatarManagerService : IAvatarManagerService
{
    private readonly ILogger<AvatarManagerService> _logger;
    private readonly WebViewService _webViewService;
    private readonly HashSet<string> _processedUserIds = new();
    private readonly SemaphoreSlim _avatarLock = new(1, 1);
    
    public ObservableCollection<AvatarPresence> OnlineAvatars { get; } = new();
    public AvatarPresence? SelectedAvatar { get; set; }
    
    public event EventHandler<AvatarPresence> AvatarAdded;
    public event EventHandler<string> AvatarRemoved;

    public AvatarManagerService(WebViewService webViewService, ILogger<AvatarManagerService> logger)
    {
        _webViewService = webViewService ?? throw new ArgumentNullException(nameof(webViewService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task EnsureAvatarPresenceAsync(string userId, string nickname, string gender, bool isCurrentUser = false)
    {
        if (!IsValidUserId(userId))
        {
            _logger.LogWarning("Invalid user ID for avatar: {UserId}", userId);
            return;
        }

        await _avatarLock.WaitAsync();
        try
        {
            if (_processedUserIds.Contains(userId))
            {
                _logger.LogDebug("Avatar {Nickname} already processed", nickname);
                return;
            }

            _processedUserIds.Add(userId);

            // Add to UI list if not already present
            if (!OnlineAvatars.Any(a => a.UserId.ToString() == userId))
            {
                var avatarPresence = new AvatarPresence
                {
                    UserId = Guid.Parse(userId),
                    Nickname = nickname,
                    AvatarConfig = AvatarConfigDto.FromJson($"{{\"gender\":\"{gender}\"}}"),
                    JoinedAt = DateTime.UtcNow,
                    IsCurrentUser = isCurrentUser
                };

                OnlineAvatars.Add(avatarPresence);
                _logger.LogDebug("✅ Avatar {Nickname} added to UI list", nickname);
            }

            // Add to 3D scene
            var success = await _webViewService.AddAvatarAsync(userId, nickname, gender);

            if (success)
            {
                _logger.LogDebug("✅ Avatar {Nickname} added to 3D scene", nickname);
                var addedAvatar = OnlineAvatars.First(a => a.UserId.ToString() == userId);
                AvatarAdded?.Invoke(this, addedAvatar);
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

    public void RemoveAvatar(string userId)
    {
        if (!IsValidUserId(userId))
        {
            _logger.LogWarning("Invalid user ID for removal: {UserId}", userId);
            return;
        }

        _avatarLock.Wait();
        try
        {
            var avatar = OnlineAvatars.FirstOrDefault(a => a.UserId.ToString() == userId);
            if (avatar != null)
            {
                OnlineAvatars.Remove(avatar);
                _logger.LogDebug("Removed avatar {UserId} from UI list", userId);
            }

            _processedUserIds.Remove(userId);
            AvatarRemoved?.Invoke(this, userId);
        }
        finally
        {
            _avatarLock.Release();
        }
    }

    public async Task UpdateSelectedAvatarFromSceneAsync()
    {
        try
        {
            var stats = await _webViewService.GetStatsAsync();
            _logger.LogDebug("WebView stats: {Stats}", stats);
            
            // Parse stats to determine selected avatar
            // TODO: Implement proper parsing when WebViewService returns structured data
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating selected avatar from scene");
        }
    }

    public async Task SyncAvatarsFromSceneAsync()
    {
        try
        {
            var avatars = await _webViewService.GetAvatarsAsync();
            
            foreach (var avatarInfo in avatars)
            {
                var existingAvatar = OnlineAvatars.FirstOrDefault(a => a.UserId.ToString() == avatarInfo.userId);
                if (existingAvatar != null)
                {
                    existingAvatar.AvatarConfig = new AvatarConfigDto { Gender = avatarInfo.gender };
                }
            }
            
            _logger.LogDebug("Synced {Count} avatars from scene", avatars.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing avatars from scene");
        }
    }

    public async Task AddSelfToSceneAsync(string userId, string nickname, string gender)
    {
        try
        {
            var success = await _webViewService.AddAvatarAsync(userId, nickname, gender, true);
            if (success)
            {
                _logger.LogInformation("✅ Current user added to 3D scene");
            }
            else
            {
                _logger.LogWarning("❌ Failed to add current user to 3D scene");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding self to scene");
        }
    }

    public void ClearProcessedUsers()
    {
        _processedUserIds.Clear();
        _logger.LogDebug("Cleared processed users tracking");
    }

    private bool IsValidUserId(string userId)
    {
        return Guid.TryParse(userId, out _);
    }
}
