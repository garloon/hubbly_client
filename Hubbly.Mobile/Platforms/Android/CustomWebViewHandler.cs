using Android.Webkit;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Controls;
using AWebView = Android.Webkit.WebView;

namespace Hubbly.Mobile.Platforms.Android;

public class CustomWebViewHandler : WebViewHandler
{
    protected override void ConnectHandler(AWebView platformView)
    {
        base.ConnectHandler(platformView);

        // Enable JavaScript
        platformView.Settings.JavaScriptEnabled = true;
        platformView.Settings.DomStorageEnabled = true;
        platformView.Settings.AllowFileAccess = true;
        platformView.Settings.AllowContentAccess = true;
        platformView.Settings.MixedContentMode = MixedContentHandling.AlwaysAllow;

        // 👇🏻 Explicitly cast to MAUI WebView
        if (VirtualView is Microsoft.Maui.Controls.WebView mauiWebView)
        {
            var bridge = new WebViewBridge(mauiWebView);
            platformView.AddJavascriptInterface(bridge, "hubblyBridge");
            Console.WriteLine("✅ CustomWebViewHandler initialized with bridge");
        }
    }
}