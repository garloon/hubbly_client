using Hubbly.Mobile.Services;
using Hubbly.Mobile.ViewModels;

namespace Hubbly.Mobile;

public partial class AppShell : Shell
{
    private readonly AppShellViewModel _viewModel;

    public AppShell(AppShellViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // Apply current theme on startup
        ApplyCurrentTheme();
    }

    private void ApplyCurrentTheme()
    {
        if (Application.Current?.Resources != null)
        {
            var themeDict = _viewModel.ThemeService.CurrentTheme;

            // Clear old theme resources
            var oldThemeKeys = Application.Current.Resources.Keys
                .Where(k => k.ToString().Contains("Color") || k.ToString().Contains("Background") || k.ToString().Contains("Text"))
                .ToList();

            foreach (var key in oldThemeKeys)
            {
                Application.Current.Resources.Remove(key);
            }

            // Add new theme resources
            foreach (var key in themeDict.Keys)
            {
                Application.Current.Resources[key] = themeDict[key];
            }
        }
    }

    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        // Customize flyout appearance
        if (Handler != null)
        {
            // Enable swipe to close
            this.SetValue(Shell.FlyoutBehaviorProperty, FlyoutBehavior.Flyout);
        }
    }
}
