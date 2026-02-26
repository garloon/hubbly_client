using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Hubbly.Mobile.Models;
using Hubbly.Mobile.Services;
using Hubbly.Mobile.Messages;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using Microsoft.Maui.Controls;

namespace Hubbly.Mobile.ViewModels;

public partial class RoomSelectionViewModel : ObservableObject, IQueryAttributable
{
    private readonly RoomService _roomService;
    private readonly TokenManager _tokenManager;
    private readonly INavigationService _navigationService;
    private readonly ILogger<RoomSelectionViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<RoomInfoDto> _rooms = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCurrentRoomSelected))]
    [NotifyPropertyChangedFor(nameof(CurrentRoomButtonText))]
    private RoomInfoDto? _selectedRoom;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _isJoining;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private RoomInfoDto? _filteredRoom;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsCurrentRoomSelected))]
    [NotifyPropertyChangedFor(nameof(CurrentRoomButtonText))]
    private Guid? _currentRoomId;

    [RelayCommand]
    private void CloseModal()
    {
        _logger.LogInformation("CloseModalCommand executed");
        CloseModalAsync().ConfigureAwait(false);
    }

    // Computed property for button text
    public string GetRoomButtonText(RoomInfoDto room)
    {
        if (CurrentRoomId.HasValue && room.RoomId == CurrentRoomId.Value)
        {
            return "Current";
        }
        return "Join";
    }

    // Computed property for button enabled state
    public bool IsRoomJoinEnabled(RoomInfoDto room)
    {
        return !(CurrentRoomId.HasValue && room.RoomId == CurrentRoomId.Value);
    }

    public RoomSelectionViewModel(RoomService roomService, TokenManager tokenManager,
                                   INavigationService navigationService, ILogger<RoomSelectionViewModel> logger)
    {
        _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
        _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("CurrentRoomId", out var currentRoomIdObj) && currentRoomIdObj is Guid currentRoomId)
        {
            CurrentRoomId = currentRoomId;
            _logger.LogInformation("Received current room ID: {CurrentRoomId}", currentRoomId);
            UpdateRoomCurrentFlags();
        }
    }
    
    partial void OnCurrentRoomIdChanged(Guid? oldValue, Guid? newValue)
    {
        // Update IsCurrent flag for all rooms when CurrentRoomId changes
        UpdateRoomCurrentFlags();
    }

    [RelayCommand]
    private async Task LoadRoomsAsync()
    {
        if (_isLoading) return;

        try
        {
            _isLoading = true;
            OnPropertyChanged(nameof(IsLoading));

            var rooms = await _roomService.GetRoomsAsync();
            Rooms.Clear();
            foreach (var room in rooms)
            {
                // Set IsCurrent flag and ButtonText for each room
                room.IsCurrent = CurrentRoomId.HasValue && room.RoomId == CurrentRoomId.Value;
                room.ButtonText = room.IsCurrent ? "Current" : "Join";
                Rooms.Add(room);
            }

            _logger.LogInformation("Loaded {RoomCount} rooms", rooms.Count());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load rooms");
            await Application.Current.MainPage.DisplayAlert("Error", "Failed to load rooms", "OK");
        }
        finally
        {
            _isLoading = false;
            OnPropertyChanged(nameof(IsLoading));
        }
    }

    [RelayCommand]
    private async Task SelectRoomAsync(RoomInfoDto? room)
    {
        // Use parameter if provided, otherwise fall back to _selectedRoom (for backward compatibility)
        var targetRoom = room ?? _selectedRoom;
        
        if (targetRoom == null)
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Please select a room", "OK");
            return;
        }

        // Check if user is trying to leave current room (either same room or different)
        if (CurrentRoomId.HasValue)
        {
            string message;
            string title;
            string confirmText;
            
            if (targetRoom.RoomId == CurrentRoomId.Value)
            {
                // Trying to join the same room
                title = "Confirm Leave";
                message = "You are already in this room. Do you want to leave and rejoin?";
                confirmText = "Yes, Leave";
            }
            else
            {
                // Trying to join a different room
                title = "Switch Room";
                message = $"You are currently in another room. Do you want to leave and join '{targetRoom.RoomName}'?";
                confirmText = "Yes, Switch";
            }
            
            var confirm = await Application.Current.MainPage.DisplayAlert(
                title,
                message,
                confirmText,
                "Cancel");
            
            if (!confirm)
                return;
        }

        try
        {
            _isJoining = true;
            OnPropertyChanged(nameof(IsJoining));

            var userIdString = await _tokenManager.GetAsync("user_id");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "User not authenticated", "OK");
                return;
            }

            var result = await _roomService.JoinRoomAsync(targetRoom.RoomId);
            
            // Проверка на null
            if (result == null)
            {
                await Application.Current.MainPage.DisplayAlert(
                    "Error",
                    "Failed to join room. The room may be full, inactive, or you lack permissions.",
                    "OK");
                return;
            }

            // Update current room ID
            await _tokenManager.SetAsync("current_room_id", result.RoomId.ToString());
            
            await Application.Current.MainPage.DisplayAlert(
                "Room Joined",
                $"Joined room: {result.RoomName}\nUsers: {result.UsersInRoom}/{result.MaxUsers}",
                "OK");

            // Close modal and navigate back to chat
            await CloseModalAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join room {RoomId}", targetRoom.RoomId);
            await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
        finally
        {
            _isJoining = false;
            OnPropertyChanged(nameof(IsJoining));
        }
    }

    private async Task CloseModalAsync()
    {
        _logger.LogInformation("Closing room selection modal");
        
        // Send message to ChatRoomViewModel to refresh room info
        WeakReferenceMessenger.Default.Send(new RoomSelectionClosedMessage());
        
        // Close modal
        if (Application.Current?.MainPage is Shell shell && shell.CurrentPage?.Navigation != null)
        {
            await shell.CurrentPage.Navigation.PopModalAsync();
        }
    }

    // Computed properties for UI
    public bool IsCurrentRoomSelected => _selectedRoom != null && CurrentRoomId.HasValue && _selectedRoom.RoomId == CurrentRoomId.Value;

    public string CurrentRoomButtonText => IsCurrentRoomSelected ? "Current" : "Join";

    // Method to check if a specific room is the current room (for XAML binding)
    public bool IsRoomCurrent(Guid roomId)
    {
        return CurrentRoomId.HasValue && roomId == CurrentRoomId.Value;
    }

    [RelayCommand]
    private async Task CreateRoomAsync()
    {
        // For now, create a simple public room
        try
        {
            var roomName = $"Room {DateTime.Now:HHmm}";
            var room = await _roomService.CreateRoomAsync(roomName, null);
            await Application.Current.MainPage.DisplayAlert(
                "Room Created",
                $"Created room: {room.RoomName}",
                "OK");

            // Reload rooms
            await LoadRoomsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create room");
            await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
    }

    partial void OnSearchTextChanged(string value)
    {
        // Filter rooms based on search text
        if (string.IsNullOrWhiteSpace(value))
        {
            FilteredRoom = null;
            return;
        }

        var lowerValue = value.ToLowerInvariant();
        var match = Rooms.FirstOrDefault(r =>
            r.RoomName.ToLowerInvariant().Contains(lowerValue) ||
            (r.Description?.ToLowerInvariant().Contains(lowerValue) == true));

        FilteredRoom = match;
    }

    private void UpdateRoomCurrentFlags()
    {
        // Update IsCurrent flag and ButtonText for all rooms in the collection
        foreach (var room in Rooms)
        {
            room.IsCurrent = CurrentRoomId.HasValue && room.RoomId == CurrentRoomId.Value;
            room.ButtonText = room.IsCurrent ? "Current" : "Join";
        }
        
        // Notify that rooms collection changed to update UI
        OnPropertyChanged(nameof(Rooms));
    }

    public async Task InitializeAsync()
    {
        await LoadRoomsAsync();
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
