namespace Hubbly.Mobile.Services;

public interface IHeartbeatService
{
    bool IsRunning { get; }
    TimeSpan Interval { get; set; }
    TimeSpan Timeout { get; set; }
    
    Task StartAsync();
    Task StopAsync();
    Task<bool> PingAsync(CancellationToken cancellationToken = default);
    
    event EventHandler? HeartbeatSucceeded;
    event EventHandler<string>? HeartbeatFailed;
    event EventHandler? HeartbeatStarted;
    event EventHandler? HeartbeatStopped;
}
