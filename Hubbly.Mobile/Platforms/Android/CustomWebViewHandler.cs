using Android.Webkit;
using Microsoft.Maui;
using Microsoft.Maui.Handlers;
using Microsoft.Maui.Controls;
using Hubbly.Mobile.Services;
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
        platformView.Settings.DatabaseEnabled = true;
        platformView.Settings.AllowFileAccess = true;
        platformView.Settings.AllowContentAccess = true;
        platformView.Settings.LoadWithOverviewMode = true;
        platformView.Settings.UseWideViewPort = true;
        platformView.Settings.MixedContentMode = MixedContentHandling.CompatibilityMode;
        
        // Enable zoom controls for better UX
        platformView.Settings.SetSupportZoom(true);
        platformView.Settings.BuiltInZoomControls = true;
        platformView.Settings.DisplayZoomControls = false;

        // Set WebChromeClient to capture console.log and errors from JavaScript
        platformView.SetWebChromeClient(new LoggingWebChromeClient());

        // 👇🏻 Explicitly cast to MAUI WebView
        if (VirtualView is Microsoft.Maui.Controls.WebView mauiWebView)
        {
            var webViewService = MauiContext.Services.GetRequiredService<WebViewService>();
            var bridge = new WebViewBridge(mauiWebView, webViewService);
            platformView.AddJavascriptInterface(bridge, "hubblyBridge");
            Console.WriteLine("✅ CustomWebViewHandler initialized with bridge");
        }
    }
}