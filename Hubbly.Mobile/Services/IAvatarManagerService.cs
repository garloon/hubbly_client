using System;
using System.Collections.ObjectModel;
using Hubbly.Mobile.Models;

namespace Hubbly.Mobile.Services;

public interface IAvatarManagerService
{
    /// <summary>
    /// Ensures an avatar is present in both the UI list and 3D scene
    /// </summary>
    Task EnsureAvatarPresenceAsync(string userId, string nickname, string gender, bool isCurrentUser = false);
    
    /// <summary>
    /// Removes an avatar from the UI list and processed tracking
    /// </summary>
    void RemoveAvatar(string userId);
    
    /// <summary>
    /// Updates the selected avatar based on the 3D scene state
    /// </summary>
    Task UpdateSelectedAvatarFromSceneAsync();
    
    /// <summary>
    /// Syncs avatars from the 3D scene to the UI collection
    /// </summary>
    Task SyncAvatarsFromSceneAsync();
    
    /// <summary>
    /// Adds the current user's avatar to the 3D scene
    /// </summary>
    Task AddSelfToSceneAsync(string userId, string nickname, string gender);
    
    /// <summary>
    /// Clears the processed users tracking
    /// </summary>
    void ClearProcessedUsers();
    
    /// <summary>
    /// Collection of online avatars
    /// </summary>
    ObservableCollection<AvatarPresence> OnlineAvatars { get; }
    
    /// <summary>
    /// Currently selected avatar
    /// </summary>
    AvatarPresence? SelectedAvatar { get; set; }
    
    /// <summary>
    /// Event raised when an avatar is added to the scene
    /// </summary>
    event EventHandler<AvatarPresence> AvatarAdded;
    
    /// <summary>
    /// Event raised when an avatar is removed from the scene
    /// </summary>
    event EventHandler<string> AvatarRemoved;
}
