using Hubbly.Mobile.Config;
using Microsoft.Extensions.Logging;

namespace Hubbly.Mobile.Services;

public class HeartbeatService : IHeartbeatService, IDisposable
{
    private readonly ILogger<HeartbeatService> _logger;
    private Timer? _timer;
    private bool _isRunning = false;
    private int _consecutiveFailures = 0;
    private const int MaxConsecutiveFailures = 3;
    
    public bool IsRunning => _isRunning;
    public TimeSpan Interval { get; set; } = AppConstants.HeartbeatInterval;
    public TimeSpan Timeout { get; set; } = AppConstants.HeartbeatTimeout;

    public event EventHandler? HeartbeatSucceeded;
    public event EventHandler<string>? HeartbeatFailed;
    public event EventHandler? HeartbeatStarted;
    public event EventHandler? HeartbeatStopped;

    public HeartbeatService(ILogger<HeartbeatService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task StartAsync()
    {
        if (_isRunning)
        {
            _logger.LogWarning("HeartbeatService: Already running");
            return;
        }

        _logger.LogInformation("HeartbeatService: Starting with interval {Interval}s", Interval.TotalSeconds);
        _isRunning = true;
        _consecutiveFailures = 0;
        
        // Start timer
        _timer = new Timer(DoHeartbeat, null, Interval, Interval);
        
        HeartbeatStarted?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (!_isRunning)
        {
            _logger.LogWarning("HeartbeatService: Not running");
            return;
        }

        _logger.LogInformation("HeartbeatService: Stopping");
        _timer?.Dispose();
        _timer = null;
        _isRunning = false;
        _consecutiveFailures = 0;
        
        HeartbeatStopped?.Invoke(this, EventArgs.Empty);
        await Task.CompletedTask;
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken = default)
    {
        // This would be called by the connection to perform a heartbeat check
        // For now, it's a placeholder that can be extended
        _logger.LogDebug("HeartbeatService: Ping requested");
        await Task.CompletedTask;
        return true;
    }

    private async void DoHeartbeat(object? state)
    {
        if (!_isRunning)
            return;

        _logger.LogTrace("HeartbeatService: Performing heartbeat check");
        
        // The actual heartbeat logic will be implemented by the caller (SignalRService)
        // This service just manages the timing and failure tracking
        
        // For now, we'll just invoke the succeeded event to keep it simple
        // In a real implementation, the caller would check connection health
        HeartbeatSucceeded?.Invoke(this, EventArgs.Empty);
        
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
        _timer = null;
        _isRunning = false;
    }
}
