using global::Android.Webkit;
using global::Android.Views;
using Microsoft.Extensions.Logging;
using System;

namespace Hubbly.Mobile.Platforms.Android;

/// <summary>
/// WebChromeClient that captures JavaScript console messages and errors
/// </summary>
public class LoggingWebChromeClient : global::Android.Webkit.WebChromeClient
{
    private readonly ILogger? _logger;

    public LoggingWebChromeClient()
    {
        // Try to get logger from service provider if available
        try
        {
            _logger = MauiProgram.ServiceProvider?.GetService<ILogger<LoggingWebChromeClient>>();
        }
        catch
        {
            _logger = null;
        }
    }

    public LoggingWebChromeClient(ILogger logger) : this()
    {
        _logger = logger ?? _logger;
    }

    /// <summary>
    /// Capture console.log, console.warn, console.error from JavaScript
    /// </summary>
    public override bool OnConsoleMessage(ConsoleMessage consoleMessage)
    {
        // Simply log all console messages - level detection is complex due to API differences
        var message = $"[JS] {consoleMessage.Message} (Line {consoleMessage.LineNumber}, Source: {consoleMessage.SourceId})";
        _logger?.LogInformation("JS Console: {Message}", message);
        return base.OnConsoleMessage(consoleMessage);
    }

    /// <summary>
    /// Capture JavaScript alerts (for debugging)
    /// </summary>
    public override bool OnJsAlert(global::Android.Webkit.WebView? view, string? url, string message, JsResult result)
    {
        _logger?.LogInformation("JS Alert: {Url} - {Message}", url, message);
        result.Confirm();
        return true; // We handled it
    }

    /// <summary>
    /// Capture JavaScript confirmations
    /// </summary>
    public override bool OnJsConfirm(global::Android.Webkit.WebView? view, string? url, string message, JsResult result)
    {
        _logger?.LogInformation("JS Confirm: {Url} - {Message}", url, message);
        result.Confirm();
        return true;
    }

    /// <summary>
    /// Capture JavaScript prompts
    /// </summary>
    public override bool OnJsPrompt(global::Android.Webkit.WebView? view, string? url, string message, string defaultValue, JsPromptResult result)
    {
        _logger?.LogInformation("JS Prompt: {Url} - {Message} (Default: {Default})", url, message, defaultValue);
        result.Confirm(defaultValue);
        return true;
    }
}
