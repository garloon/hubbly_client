using CommunityToolkit.Mvvm.ComponentModel;

namespace Hubbly.Mobile.Models;

public partial class ChatMessage : ObservableObject
{
    [ObservableProperty]
    private string _senderId = string.Empty;

    [ObservableProperty]
    private string _senderNickname = string.Empty;

    [ObservableProperty]
    private string _content = string.Empty;

    [ObservableProperty]
    private DateTimeOffset _sentAt;

    [ObservableProperty]
    private bool _isCurrentUser;

    public bool IsSystemMessage => SenderId == "system";
    public bool IsNotSystemMessage => SenderId != "system";
    public bool IsOtherUserMessage => IsNotSystemMessage && !IsCurrentUser;

    // Added for debugging
    public override string ToString()
    {
        return $"{_senderNickname}: {_content}";
    }
}
