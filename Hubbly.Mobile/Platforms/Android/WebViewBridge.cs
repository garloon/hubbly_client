using Android.Runtime;
using Android.Webkit;
using Hubbly.Mobile.Services;
using Java.Interop;
using Microsoft.Maui.Controls;
using AWebView = Android.Webkit.WebView;
using MWebView = Microsoft.Maui.Controls.WebView;

namespace Hubbly.Mobile.Platforms.Android;

public class WebViewBridge : Java.Lang.Object
{
    private readonly WeakReference<MWebView> _webView;

    public WebViewBridge(MWebView webView) 
    {
        _webView = new WeakReference<MWebView>(webView);
    }

    [JavascriptInterface]
    [Export("postMessage")]
    public void PostMessage(string jsonMessage)
    {
        if (jsonMessage?.Length > 10000)
        {
            Console.WriteLine("⚠️ Bridge message too large, ignoring");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(jsonMessage) ||
            (!jsonMessage.StartsWith("{") && !jsonMessage.StartsWith("[")))
        {
            Console.WriteLine("⚠️ Bridge message not JSON, ignoring");
            return;
        }

        Console.WriteLine($"🔵 Bridge received: {jsonMessage}");

        MainThread.BeginInvokeOnMainThread(() =>
        {
            try
            {
                var webViewService = MauiProgram.ServiceProvider.GetRequiredService<WebViewService>();
                webViewService.HandleJsMessage(jsonMessage);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Bridge error: {ex.Message}");
            }
        });
    }
}