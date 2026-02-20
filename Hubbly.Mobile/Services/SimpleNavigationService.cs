using Hubbly.Mobile.Views;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Services;

public class SimpleNavigationService : INavigationService, IDisposable
{
    private readonly ILogger<SimpleNavigationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly SemaphoreSlim _navigationLock = new(1, 1);
    private readonly CancellationTokenSource _cts = new();

    private bool _disposed;

    public SimpleNavigationService(IServiceProvider serviceProvider, ILogger<SimpleNavigationService> logger)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger;
    }

    #region Public Methods

    public async Task NavigateToAsync(string route)
    {
        await NavigateToAsync(route, null);
    }

    public async Task NavigateToAsync(string route, IDictionary<string, object> parameters)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(route))
            throw new ArgumentNullException(nameof(route));

        _logger.LogInformation("SimpleNavigationService: Navigating to {Route}", route);

        if (!await _navigationLock.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            _logger.LogError("NavigationService: Failed to acquire lock within 5 seconds");
            return;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    if (Application.Current?.MainPage is not NavigationPage navigationPage)
                    {
                        _logger.LogError("NavigationService: MainPage is not NavigationPage. Type: {Type}",
                            Application.Current?.MainPage?.GetType().Name);
                        return;
                    }

                    _logger.LogDebug("NavigationService: Current stack size: {StackSize}",
                        navigationPage.Navigation.NavigationStack.Count);

                    Page page = route switch
                    {
                        "//ChatRoomPage" => _serviceProvider.GetRequiredService<ChatRoomPage>(),
                        "//WelcomePage" => _serviceProvider.GetRequiredService<WelcomePage>(),
                        "//AvatarSelectionPage" => _serviceProvider.GetRequiredService<AvatarSelectionPage>(),
                        _ => throw new ArgumentException($"Unknown route: {route}")
                    };

                    // Pass parameters if any
                    if (parameters != null && page.BindingContext is IQueryAttributable attributable)
                    {
                        attributable.ApplyQueryAttributes(parameters);
                    }

                    await navigationPage.Navigation.PushAsync(page);

                    _logger.LogInformation("✅ NavigationService: Navigated to {Route}", route);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "NavigationService: Failed to navigate to {Route}", route);
                    throw;
                }
            });
        }
        finally
        {
            _navigationLock.Release();
        }
    }

    public async Task GoBackAsync()
    {
        ThrowIfDisposed();

        _logger.LogInformation("SimpleNavigationService: Going back");

        if (!await _navigationLock.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            _logger.LogError("NavigationService: Failed to acquire lock within 5 seconds");
            return;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    if (Application.Current?.MainPage is not NavigationPage navigationPage)
                    {
                        _logger.LogError("NavigationService: MainPage is not NavigationPage");
                        return;
                    }

                    if (navigationPage.Navigation.NavigationStack.Count <= 1)
                    {
                        _logger.LogWarning("NavigationService: Cannot go back - at root page");
                        return;
                    }

                    await navigationPage.Navigation.PopAsync();

                    _logger.LogInformation("✅ NavigationService: Went back");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "NavigationService: Failed to go back");
                    throw;
                }
            });
        }
        finally
        {
            _navigationLock.Release();
        }
    }

    public async Task NavigateToRootAsync()
    {
        ThrowIfDisposed();

        _logger.LogInformation("SimpleNavigationService: Navigating to root");

        if (!await _navigationLock.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            _logger.LogError("NavigationService: Failed to acquire lock within 5 seconds");
            return;
        }

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    if (Application.Current?.MainPage is not NavigationPage navigationPage)
                    {
                        _logger.LogError("NavigationService: MainPage is not NavigationPage");
                        return;
                    }

                    await navigationPage.Navigation.PopToRootAsync();

                    _logger.LogInformation("✅ NavigationService: Navigated to root");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "NavigationService: Failed to navigate to root");
                    throw;
                }
            });
        }
        finally
        {
            _navigationLock.Release();
        }
    }

    public async Task<Page> GetCurrentPageAsync()
    {
        ThrowIfDisposed();

        return await MainThread.InvokeOnMainThreadAsync(() =>
        {
            if (Application.Current?.MainPage is not NavigationPage navigationPage)
            {
                return null;
            }

            return navigationPage.Navigation.NavigationStack.LastOrDefault();
        });
    }

    #endregion

    #region Private Methods

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(SimpleNavigationService));
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("SimpleNavigationService: Disposing...");

        _cts.Cancel();
        _cts.Dispose();
        _navigationLock.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("SimpleNavigationService: Disposed");
    }

    #endregion
}