using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hubbly.Mobile.Models;
using Hubbly.Mobile.Services;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace Hubbly.Mobile.ViewModels;

public partial class RoomSelectionViewModel : ObservableObject
{
    private readonly RoomService _roomService;
    private readonly TokenManager _tokenManager;
    private readonly ILogger<RoomSelectionViewModel> _logger;

    [ObservableProperty]
    private ObservableCollection<RoomInfoDto> _rooms = new();

    [ObservableProperty]
    private RoomInfoDto? _selectedRoom;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private RoomInfoDto? _filteredRoom;

    public RoomSelectionViewModel(RoomService roomService, TokenManager tokenManager, ILogger<RoomSelectionViewModel> logger)
    {
        _roomService = roomService ?? throw new ArgumentNullException(nameof(roomService));
        _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
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
    private async Task SelectRoomAsync()
    {
        if (_selectedRoom == null)
        {
            await Application.Current.MainPage.DisplayAlert("Error", "Please select a room", "OK");
            return;
        }

        try
        {
            var userIdString = await _tokenManager.GetAsync("user_id");
            if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            {
                await Application.Current.MainPage.DisplayAlert("Error", "User not authenticated", "OK");
                return;
            }

            var result = await _roomService.JoinRoomAsync(_selectedRoom.RoomId);
            await Application.Current.MainPage.DisplayAlert(
                "Room Joined",
                $"Joined room: {result.RoomName}\nUsers: {result.UsersInRoom}/{result.MaxUsers}",
                "OK");

            // Navigate back to chat room
            await Shell.Current.GoToAsync("//chatroom");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to join room {RoomId}", _selectedRoom.RoomId);
            await Application.Current.MainPage.DisplayAlert("Error", ex.Message, "OK");
        }
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
