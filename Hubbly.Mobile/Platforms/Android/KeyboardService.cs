using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Views.InputMethods;
using Hubbly.Mobile.Services;
using Microsoft.Maui;
using Microsoft.Maui.Controls;

namespace Hubbly.Mobile.Platforms.Android;

public class KeyboardService : IKeyboardService
{
    public void HideKeyboard()
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var activity = Platform.CurrentActivity;
            var inputMethodManager = activity.GetSystemService(Context.InputMethodService)
                as InputMethodManager;
            var currentFocus = activity.CurrentFocus;
            if (currentFocus != null)
            {
                inputMethodManager?.HideSoftInputFromWindow(currentFocus.WindowToken, 0);
                currentFocus.ClearFocus();
            }
        });
    }
}
