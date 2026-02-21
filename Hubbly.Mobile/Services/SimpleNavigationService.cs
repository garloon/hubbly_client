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
                    if (Application.Current?.MainPage is not Shell shell)
                    {
                        _logger.LogError("NavigationService: MainPage is not Shell. Type: {Type}",
                            Application.Current?.MainPage?.GetType().Name);
                        return;
                    }

                    _logger.LogDebug("NavigationService: Using Shell navigation to {Route}", route);

                    // Check if this should be a modal navigation
                    // Modal routes: settings, about (show as overlay without losing chat state)
                    if (route.StartsWith("//settings") || route.StartsWith("//about"))
                    {
                        _logger.LogInformation("🔍 NavigationService: Detected modal route {Route}", route);
                        
                        // Resolve the page from DI container
                        var pageType = GetPageTypeFromRoute(route);
                        if (pageType == null)
                        {
                            _logger.LogError("NavigationService: Cannot resolve page type for route {Route}", route);
                            return;
                        }

                        var page = _serviceProvider.GetRequiredService(pageType) as Page;
                        if (page == null)
                        {
                            _logger.LogError("NavigationService: Failed to create page for route {Route}", route);
                            return;
                        }

                        _logger.LogInformation("🔍 NavigationService: Created page {PageType} for modal", pageType.Name);

                        // Show as modal
                        _logger.LogInformation("🔍 NavigationService: About to call PushModalAsync");
                        await shell.CurrentPage.Navigation.PushModalAsync(page);
                        _logger.LogInformation("✅ NavigationService: Opened modal page for route {Route}", route);
                    }
                    else
                    {
                        // Regular Shell navigation
                        await shell.GoToAsync(route);
                        _logger.LogInformation("✅ NavigationService: Navigated to {Route}", route);
                    }
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

    private Type GetPageTypeFromRoute(string route)
    {
        // Map routes to page types
        // Note: route may contain query parameters, so we need to parse
        var cleanRoute = route.Split('?')[0].TrimStart('/');
        
        return cleanRoute.ToLowerInvariant() switch
        {
            "settings" => typeof(SettingsPage),
            "about" => typeof(AboutPage),
            _ => null
        };
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
                    if (Application.Current?.MainPage is not Shell shell)
                    {
                        _logger.LogError("NavigationService: MainPage is not Shell");
                        return;
                    }

                    // Check if we can go back
                    if (shell.Navigation.NavigationStack.Count <= 1)
                    {
                        _logger.LogWarning("NavigationService: Cannot go back - at root page");
                        return;
                    }

                    await shell.Navigation.PopAsync();

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
                    if (Application.Current?.MainPage is not Shell shell)
                    {
                        _logger.LogError("NavigationService: MainPage is not Shell");
                        return;
                    }

                    // Navigate to root (welcome page)
                    await shell.GoToAsync("//welcome");

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
            if (Application.Current?.MainPage is not Shell shell)
            {
                return null;
            }

            return shell.CurrentPage;
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