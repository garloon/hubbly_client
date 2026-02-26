using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Hubbly.Mobile.Config;
using Hubbly.Mobile.Models;
using Hubbly.Mobile.Services;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.ViewModels;

public partial class AvatarSelectionViewModel : ObservableObject, IDisposable, IQueryAttributable
{
    private readonly ILogger<AvatarSelectionViewModel> _logger;
    private readonly AuthService _authService;
    private readonly INavigationService _navigationService;
    private readonly TokenManager _tokenManager;
    private readonly DeviceIdService _deviceIdService;
    private readonly SemaphoreSlim _navigationLock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();

    private bool _disposed;

    [ObservableProperty]
    private string _selectedGender = "male";

    [ObservableProperty]
    private bool _isBusy = false;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    // Computed properties
    public string AvatarEmoji => SelectedGender == "male" ? "👨" : "👩";
    public string GenderText => SelectedGender == "male" ? "MALE" : "FEMALE";
    public bool IsMaleSelected => SelectedGender == "male";
    public bool IsFemaleSelected => SelectedGender == "female";
    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public AvatarSelectionViewModel(
        AuthService authService,
        INavigationService navigationService,
        TokenManager tokenManager,
        DeviceIdService deviceIdService,
        ILogger<AvatarSelectionViewModel> logger)
    {
        _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        _navigationService = navigationService ?? throw new ArgumentNullException(nameof(navigationService));
        _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
        _deviceIdService = deviceIdService ?? throw new ArgumentNullException(nameof(deviceIdService));

        _logger = logger;

        _logger.LogInformation("AvatarSelectionViewModel created");
    }

    #region Commands

    [RelayCommand]
    private void SelectMale()
    {
        try
        {
            SelectedGender = "male";
            ErrorMessage = string.Empty;
            _logger.LogDebug("Gender selected: male");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SelectMale");
        }
    }

    [RelayCommand]
    private void SelectFemale()
    {
        try
        {
            SelectedGender = "female";
            ErrorMessage = string.Empty;
            _logger.LogDebug("Gender selected: female");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SelectFemale");
        }
    }

    [RelayCommand]
    private async Task Confirm()
    {
        // Protection against repeated clicks
        if (IsBusy)
        {
            _logger.LogWarning("Confirm: Already busy, ignoring click");
            return;
        }

        if (!await _navigationLock.WaitAsync(TimeSpan.FromSeconds(AppConstants.NavigationLockTimeoutSeconds)))
        {
            _logger.LogError("Confirm: Failed to acquire lock within 5 seconds");
            ErrorMessage = "System busy, please try again";
            return;
        }

        try
        {
            IsBusy = true;
            ErrorMessage = string.Empty;

            _logger.LogInformation("Starting avatar confirmation process with gender: {Gender}", SelectedGender);

            // 1. Check server
            var isAvailable = await _authService.CheckServerHealthAsync();
            if (!isAvailable)
            {
                ErrorMessage = "Cannot connect to Hubbly. Please check your internet connection.";
                _logger.LogWarning("Server unavailable");
                return;
            }

            // 2. Create avatar config
            var avatarConfig = new AvatarConfigDto
            {
                Gender = SelectedGender,
                BaseModelId = $"{SelectedGender}_base",
                Pose = "standing"
            };
            var configJson = avatarConfig.ToJson();

            // 3. Authenticate
            _logger.LogInformation("Authenticating as guest with avatar");

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(_cts.Token);
            cts.CancelAfter(TimeSpan.FromSeconds(AppConstants.ConnectionLockTimeoutSeconds));

            var authResponse = await _authService.AuthenticateGuestWithAvatarAsync(configJson);

            // 4. Save tokens
            await _tokenManager.SetAsync("access_token", authResponse.AccessToken, AppConstants.AccessTokenExpiration);
            await _tokenManager.SetAsync("refresh_token", authResponse.RefreshToken, AppConstants.RefreshTokenExpiration);
            await _tokenManager.SetAsync("user_id", authResponse.User.Id.ToString());

            // 5. Save user data (encrypted)
            if (!string.IsNullOrEmpty(authResponse.User.Nickname))
            {
                await _tokenManager.SetEncryptedAsync("nickname", authResponse.User.Nickname);
            }

            await _tokenManager.SetEncryptedAsync("avatar_config", configJson);
            // last_device_id is not sensitive, can stay unencrypted in Preferences
            Preferences.Set("last_device_id", _deviceIdService.GetPersistentDeviceId());

            _logger.LogInformation("✅ Avatar created successfully for user {UserId}", authResponse.User.Id);

            // 6. Navigate to chat
            await _navigationService.NavigateToAsync("//chat");
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Confirm operation timed out");
            ErrorMessage = "Request timed out. Please try again.";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Network error during confirmation");
            ErrorMessage = "Network error. Please check your connection.";
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Authentication failed");
            ErrorMessage = "Authentication failed. Please try again.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ AvatarSelection Error");
            ErrorMessage = $"Failed to create avatar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
            _navigationLock.Release();
        }
    }

    [RelayCommand]
    private async Task GoBack()
    {
        try
        {
            _logger.LogInformation("Going back to welcome page");
            await _navigationService.GoBackAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error going back");
            ErrorMessage = "Failed to navigate back";
        }
    }

    [RelayCommand]
    private void ClearError()
    {
        ErrorMessage = string.Empty;
    }

    #endregion

    #region IQueryAttributable

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        try
        {
            if (query.TryGetValue("gender", out var gender))
            {
                SelectedGender = gender.ToString() == "female" ? "female" : "male";
                _logger.LogDebug("Applied gender from query: {Gender}", SelectedGender);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error applying query attributes");
        }
    }

    #endregion

    #region Lifecycle

    partial void OnSelectedGenderChanged(string value)
    {
        OnPropertyChanged(nameof(IsMaleSelected));
        OnPropertyChanged(nameof(IsFemaleSelected));
        OnPropertyChanged(nameof(AvatarEmoji));
        OnPropertyChanged(nameof(GenderText));

        _logger.LogTrace("Gender changed to: {Gender}", value);
    }

    public void OnAppearing()
    {
        _logger.LogDebug("AvatarSelectionPage appearing");
        ErrorMessage = string.Empty;
    }

    public void OnDisappearing()
    {
        _logger.LogDebug("AvatarSelectionPage disappearing");
        _cts.Cancel();
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("AvatarSelectionViewModel disposing...");

        _cts.Cancel();
        _cts.Dispose();
        _navigationLock.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("AvatarSelectionViewModel disposed");
    }

    #endregion
}