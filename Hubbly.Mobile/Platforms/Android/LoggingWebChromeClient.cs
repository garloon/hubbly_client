using Android.Webkit;
using Microsoft.Extensions.Logging;
using System;

namespace Hubbly.Mobile.Platforms.Android;

/// <summary>
/// WebChromeClient that captures JavaScript console messages and errors
/// </summary>
public class LoggingWebChromeClient : WebChromeClient
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
        var message = $"[JS {consoleMessage.MessageLevel()}] {consoleMessage.Message()} (Line {consoleMessage.LineNumber()}, Source: {consoleMessage.SourceId()})";
        
        switch (consoleMessage.MessageLevel())
        {
            case ConsoleMessage.MessageLevel.Error:
                _logger.LogError("JS Console Error: {Message}", message);
                break;
            case ConsoleMessage.MessageLevel.Warning:
                _logger.LogWarning("JS Console Warning: {Message}", message);
                break;
            case ConsoleMessage.MessageLevel.Log:
                _logger.LogInformation("JS Console Log: {Message}", message);
                break;
            case ConsoleMessage.MessageLevel.Debug:
                _logger.LogDebug("JS Console Debug: {Message}", message);
                break;
            default:
                _logger.LogInformation("JS Console: {Message}", message);
                break;
        }
        
        return base.OnConsoleMessage(consoleMessage);
    }

    /// <summary>
    /// Capture JavaScript alerts (for debugging)
    /// </summary>
    public override bool OnJsAlert(Android.Webkit.WebView? view, string? url, string message, JsResult result)
    {
        _logger.LogInformation("JS Alert: {Url} - {Message}", url, message);
        result.Confirm();
        return true; // We handled it
    }

    /// <summary>
    /// Capture JavaScript confirmations
    /// </summary>
    public override bool OnJsConfirm(Android.Webkit.WebView? view, string? url, string message, JsResult result)
    {
        _logger.LogInformation("JS Confirm: {Url} - {Message}", url, message);
        result.Confirm();
        return true;
    }

    /// <summary>
    /// Capture JavaScript prompts
    /// </summary>
    public override bool OnJsPrompt(Android.Webkit.WebView? view, string? url, string message, string defaultValue, JsPromptResult result)
    {
        _logger.LogInformation("JS Prompt: {Url} - {Message} (Default: {Default})", url, message, defaultValue);
        result.Confirm(defaultValue);
        return true;
    }

    /// <summary>
    /// Capture uncaught JavaScript exceptions
    /// </summary>
    public override void OnUnhandledKeyEvent(Android.Views.KeyEvent? e)
    {
        if (e?.KeyCode == Android.Views.Keycode.Unknown)
        {
            _logger.LogWarning("Unhandled key event in WebView");
        }
        base.OnUnhandledKeyEvent(e);
    }
}
