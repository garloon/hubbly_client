using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Hubbly.Mobile.Services;

public class WebViewService : IDisposable
{
    private readonly ILogger<WebViewService> _logger;
    private readonly SemaphoreSlim _avatarLock = new(1, 1);
    private readonly SemaphoreSlim _sceneLock = new(1, 1);
    private readonly Queue<Func<Task<bool>>> _pendingAvatarQueue = new();
    private readonly CancellationTokenSource _cts = new();
    private readonly HashSet<string> _allowedMessageTypes = new()
    {
        "scene_ready",
        "webgl_not_supported",
        "avatar_added",
        "avatar_removed",
        "avatar_clicked",
        "model_loaded",
        "animation_started",
        "animation_ended"
    };

    private WebView _webView;
    private bool _isInitialized;
    private bool _isSceneReady;
    private bool _isProcessingQueue;
    private bool _disposed;
    private Timer _queueTimer;

    // События
    public event EventHandler<string> OnSceneReady;
    public event EventHandler<string> OnSceneError;
    public event EventHandler<JsonElement> OnWebViewMessage;
    public event EventHandler<string> OnAvatarClicked;

    // Свойства
    public bool IsSceneReady => _isSceneReady;
    public bool IsInitialized => _isInitialized;

    public WebViewService(ILogger<WebViewService> logger)
    {
        _logger = logger;

        // Таймер для обработки очереди (каждые 2 секунды)
        _queueTimer = new Timer(ProcessAvatarQueue, null,
            TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2));
    }

    #region Публичные методы

    public void Initialize(WebView webView)
    {
        ThrowIfDisposed();

        if (_isInitialized)
        {
            _logger.LogWarning("WebViewService already initialized");
            return;
        }

        _webView = webView ?? throw new ArgumentNullException(nameof(webView));
        _isInitialized = true;

        _logger.LogInformation("WebViewService: Initialized with Three.js scene");

        // Подписываемся на события навигации
        _webView.Navigated += OnWebViewNavigated;
    }

    public async Task<bool> AddAvatarAsync(string userId, string nickname, string gender, bool isCurrentUser = false)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(nickname) || string.IsNullOrEmpty(gender))
        {
            _logger.LogError("AddAvatarAsync: Invalid parameters - userId:{UserId}, nickname:{Nickname}, gender:{Gender}",
                userId, nickname, gender);
            return false;
        }

        if (!_isSceneReady)
        {
            _logger.LogInformation($"WebViewService: Scene not ready, queueing avatar {nickname}");

            lock (_pendingAvatarQueue)
            {
                _pendingAvatarQueue.Enqueue(() => AddAvatarAsync(userId, nickname, gender, isCurrentUser));
            }

            return false;
        }

        if (!await _avatarLock.WaitAsync(TimeSpan.FromSeconds(5)))
        {
            _logger.LogError("AddAvatarAsync: Failed to acquire lock within 5 seconds");
            return false;
        }

        try
        {
            // Проверяем, не существует ли уже такой аватар
            var existingAvatars = await GetAvatarsAsync(_cts.Token);
            if (existingAvatars.Any(a => a.userId == userId))
            {
                _logger.LogInformation($"⚠️ Avatar {nickname} already exists, skipping");
                return true;
            }

            _logger.LogInformation($"WebViewService: Adding avatar: {nickname} ({gender})");
            var result = await AddAvatarInternalAsync(userId, nickname, gender, isCurrentUser);
            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("AddAvatarAsync cancelled");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddAvatarAsync failed for user {UserId}", userId);
            return false;
        }
        finally
        {
            _avatarLock.Release();
        }
    }

    public async Task<bool> RemoveAvatarAsync(string userId)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("RemoveAvatarAsync: userId is empty");
            return false;
        }

        if (!_isSceneReady)
        {
            _logger.LogWarning("RemoveAvatarAsync: Scene not ready");
            return false;
        }

        try
        {
            var safeUserId = EscapeJsString(userId);

            var js = $@"
                (function() {{
                    try {{
                        if (!window.hubbly3d || !window.hubbly3d.removeAvatar) {{
                            return 'API_NOT_AVAILABLE';
                        }}
                        var result = window.hubbly3d.removeAvatar('{safeUserId}');
                        return result ? 'REMOVED' : 'NOT_FOUND';
                    }} catch(e) {{
                        return 'ERROR: ' + e.message;
                    }}
                }})();
            ";

            var result = await EvaluateJavaScriptAsync(js, _cts.Token);
            return result == "REMOVED";
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("RemoveAvatarAsync cancelled");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RemoveAvatarAsync failed for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> ClearAvatarsAsync()
    {
        ThrowIfDisposed();

        if (!_isSceneReady)
        {
            _logger.LogWarning("ClearAvatarsAsync: Scene not ready");
            return false;
        }

        try
        {
            var js = @"
                (function() {
                    try {
                        if (!window.hubbly3d || !window.hubbly3d.clearAvatars) {
                            return 'API_NOT_AVAILABLE';
                        }
                        window.hubbly3d.clearAvatars();
                        return 'CLEARED';
                    } catch(e) {
                        return 'ERROR: ' + e.message;
                    }
                })();
            ";

            var result = await EvaluateJavaScriptAsync(js, _cts.Token);

            // Очищаем очередь
            lock (_pendingAvatarQueue)
            {
                _pendingAvatarQueue.Clear();
            }

            return result == "CLEARED";
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("ClearAvatarsAsync cancelled");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ClearAvatarsAsync failed");
            return false;
        }
    }

    public async Task<bool> PlayAnimationAsync(string userId, string animationName, bool loop = false)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(animationName))
        {
            _logger.LogError("PlayAnimationAsync: Invalid parameters");
            return false;
        }

        if (!_isSceneReady)
        {
            _logger.LogWarning($"PlayAnimationAsync: Scene not ready, cannot play animation for {userId}");
            return false;
        }

        try
        {
            var safeUserId = EscapeJsString(userId);
            var safeAnimation = EscapeJsString(animationName);

            var js = $@"
                (function() {{
                    try {{
                        if (!window.hubbly3d || !window.hubbly3d.playAnimation) {{
                            return 'API_NOT_AVAILABLE';
                        }}
                        window.hubbly3d.playAnimation('{safeUserId}', '{safeAnimation}', {loop.ToString().ToLower()});
                        return 'SUCCESS';
                    }} catch(e) {{
                        return 'ERROR: ' + e.message;
                    }}
                }})();
            ";

            var result = await EvaluateJavaScriptAsync(js, _cts.Token);
            var success = result == "SUCCESS";

            if (success)
            {
                _logger.LogInformation($"✅ WebViewService: Playing animation {animationName} for user {userId}");
            }
            else
            {
                _logger.LogWarning($"❌ WebViewService: Failed to play animation {animationName} for user {userId}: {result}");
            }

            return success;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("PlayAnimationAsync cancelled");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PlayAnimationAsync failed for user {UserId}", userId);
            return false;
        }
    }

    public async Task<bool> StopAnimationAsync(string userId)
    {
        ThrowIfDisposed();

        if (string.IsNullOrEmpty(userId))
        {
            _logger.LogError("StopAnimationAsync: userId is empty");
            return false;
        }

        if (!_isSceneReady)
        {
            _logger.LogWarning("StopAnimationAsync: Scene not ready");
            return false;
        }

        try
        {
            var safeUserId = EscapeJsString(userId);

            var js = $@"
                (function() {{
                    try {{
                        if (!window.hubbly3d || !window.hubbly3d.stopAnimation) {{
                            return 'API_NOT_AVAILABLE';
                        }}
                        window.hubbly3d.stopAnimation('{safeUserId}');
                        return 'SUCCESS';
                    }} catch(e) {{
                        return 'ERROR: ' + e.message;
                    }}
                }})();
            ";

            var result = await EvaluateJavaScriptAsync(js, _cts.Token);
            return result == "SUCCESS";
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("StopAnimationAsync cancelled");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "StopAnimationAsync failed for user {UserId}", userId);
            return false;
        }
    }

    public async Task<string> GetStatsAsync()
    {
        ThrowIfDisposed();

        try
        {
            var js = @"
                (function() {
                    try {
                        if (!window.hubbly3d || !window.hubbly3d.getStats) {
                            return JSON.stringify({ error: 'API not available' });
                        }
                        return JSON.stringify(window.hubbly3d.getStats());
                    } catch(e) {
                        return JSON.stringify({ error: e.message });
                    }
                })();
            ";

            var result = await EvaluateJavaScriptAsync(js, _cts.Token);
            return result ?? "{ \"error\": \"no response\" }";
        }
        catch (OperationCanceledException)
        {
            return "{ \"error\": \"cancelled\" }";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetStatsAsync failed");
            return $"{{\"error\": \"{ex.Message}\"}}";
        }
    }

    public async Task<List<AvatarInfo>> GetAvatarsAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();

        try
        {
            var js = @"
                (function() {
                    try {
                        if (!window.hubbly3d || !window.hubbly3d.getAvatars) {
                            return '[]';
                        }
                        return JSON.stringify(window.hubbly3d.getAvatars());
                    } catch(e) {
                        return '[]';
                    }
                })();
            ";

            var result = await EvaluateJavaScriptAsync(js, cancellationToken);
            return JsonSerializer.Deserialize<List<AvatarInfo>>(result ?? "[]") ?? new();
        }
        catch (OperationCanceledException)
        {
            return new();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetAvatarsAsync failed");
            return new();
        }
    }

    public void HandleJsMessage(string message)
    {
        ThrowIfDisposed();

        try
        {
            // Ограничиваем размер
            if (message?.Length > 10000)
            {
                _logger.LogWarning("HandleJsMessage: Message too large ({Length} bytes)", message?.Length ?? 0);
                return;
            }

            _logger.LogDebug($"WebViewService: JS Message: {message}");

            using JsonDocument doc = JsonDocument.Parse(message);
            var root = doc.RootElement;

            // Проверяем наличие обязательных полей
            if (!root.TryGetProperty("type", out var typeElement))
            {
                _logger.LogWarning("HandleJsMessage: Missing 'type' field");
                return;
            }

            var typeStr = typeElement.GetString();
            if (string.IsNullOrEmpty(typeStr))
            {
                _logger.LogWarning("HandleJsMessage: Empty type field");
                return;
            }

            // Проверяем, разрешен ли такой тип сообщения
            if (!_allowedMessageTypes.Contains(typeStr))
            {
                _logger.LogWarning($"HandleJsMessage: Invalid message type: {typeStr}");
                return;
            }

            // Обрабатываем разные типы сообщений
            switch (typeStr)
            {
                case "scene_ready":
                    _logger.LogInformation("WebViewService: Scene ready from JS");
                    _isSceneReady = true;
                    OnSceneReady?.Invoke(this, message);
                    break;

                case "webgl_not_supported":
                    _logger.LogError("WebViewService: WebGL not supported");
                    OnSceneError?.Invoke(this, "WebGL not supported");
                    break;

                case "avatar_clicked":
                    HandleAvatarClicked(root);
                    break;

                case "avatar_added":
                case "avatar_removed":
                case "model_loaded":
                case "animation_started":
                case "animation_ended":
                    _logger.LogDebug($"WebViewService: {typeStr}");
                    break;

                default:
                    _logger.LogWarning($"WebViewService: Unhandled message type: {typeStr}");
                    break;
            }

            OnWebViewMessage?.Invoke(this, root);
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "WebViewService: Invalid JSON: {Message}", message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "WebViewService: HandleJsMessage error");
        }
    }

    public void Cleanup()
    {
        if (_disposed) return;

        _logger.LogInformation("WebViewService: Cleaning up...");

        // Отменяем все операции
        _cts.Cancel();

        // Отписываемся от событий
        if (_webView != null)
        {
            _webView.Navigated -= OnWebViewNavigated;
            _webView = null;
        }

        // Очищаем очередь
        lock (_pendingAvatarQueue)
        {
            _pendingAvatarQueue.Clear();
        }

        _isInitialized = false;
        _isSceneReady = false;

        _logger.LogInformation("WebViewService: Cleaned up");
    }

    #endregion

    #region Приватные методы

    private async void OnWebViewNavigated(object sender, WebNavigatedEventArgs e)
    {
        try
        {
            _logger.LogInformation($"WebViewService: Navigated to {e.Url}, Result: {e.Result}");

            if (e.Result == WebNavigationResult.Success)
            {
                // Даем время Three.js загрузиться
                await Task.Delay(2000, _cts.Token);

                // Проверяем готовность сцены
                await CheckSceneStatus();
            }
            else
            {
                OnSceneError?.Invoke(this, $"Navigation failed: {e.Result}");
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("OnWebViewNavigated cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OnWebViewNavigated error");
            OnSceneError?.Invoke(this, ex.Message);
        }
    }

    private async Task<bool> CheckSceneStatus()
    {
        try
        {
            var js = @"
                (function() {
                    try {
                        return window.hubbly3d ? window.hubbly3d.isReady() : false;
                    } catch(e) {
                        return false;
                    }
                })();
            ";

            var readyCheck = await EvaluateJavaScriptAsync(js, _cts.Token);
            _logger.LogDebug($"WebViewService: Scene ready: {readyCheck}");

            _isSceneReady = readyCheck == "true" || readyCheck == "True";

            if (_isSceneReady)
            {
                _logger.LogInformation("WebViewService: ✅ 3D SCENE READY!");
                OnSceneReady?.Invoke(this, "3D scene ready");
                return true;
            }

            return false;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("CheckSceneStatus cancelled");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CheckSceneStatus error");
            OnSceneError?.Invoke(this, ex.Message);
            return false;
        }
    }

    private async Task<string> EvaluateJavaScriptAsync(string script, CancellationToken cancellationToken)
    {
        if (!_isInitialized || _webView == null)
        {
            _logger.LogWarning("WebViewService: Not initialized");
            return string.Empty;
        }

        try
        {
            // Ограничиваем логирование длинных скриптов
            var logScript = script.Length > 100 ? script.Substring(0, 100) + "..." : script;
            _logger.LogDebug($"WebViewService: Executing JS: {logScript}");

            string result = string.Empty;

            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                try
                {
                    using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    cts.CancelAfter(TimeSpan.FromSeconds(10)); // Таймаут 10 секунд

                    var jsResult = await _webView.EvaluateJavaScriptAsync(script);
                    result = jsResult?.ToString() ?? string.Empty;

                    _logger.LogDebug($"WebViewService: JS Result: {result}");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("EvaluateJavaScriptAsync timeout");
                    result = "ERROR: Timeout";
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WebViewService: JS Error");
                    result = $"ERROR: {ex.Message}";
                }
            });

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "EvaluateJavaScriptAsync error");
            return $"ERROR: {ex.Message}";
        }
    }

    private async Task<bool> AddAvatarInternalAsync(string userId, string nickname, string gender, bool isCurrentUser)
    {
        try
        {
            var safeNickname = EscapeJsString(nickname);
            var safeUserId = EscapeJsString(userId);

            var js = $@"
                (function() {{
                    try {{
                        if (!window.hubbly3d || !window.hubbly3d.addAvatar) {{
                            return 'API_NOT_AVAILABLE';
                        }}
                        
                        // Проверяем, есть ли уже такой аватар
                        var exists = window.hubbly3d.getAvatars?.().some(a => a.userId === '{safeUserId}');
                        if (exists) {{
                            console.log('JS: Avatar {safeNickname} already exists');
                            return 'SUCCESS';
                        }}
                        
                        var result = window.hubbly3d.addAvatar(
                            '{safeUserId}', 
                            '{safeNickname}', 
                            '{gender}', 
                            {isCurrentUser.ToString().ToLower()}
                        );
                        
                        return result ? 'SUCCESS' : 'FAILED';
                    }} catch(e) {{
                        console.error('JS: Error:', e);
                        return 'ERROR: ' + e.message;
                    }}
                }})();
            ";

            var result = await EvaluateJavaScriptAsync(js, _cts.Token);
            return result == "SUCCESS";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "AddAvatarInternalAsync failed");
            return false;
        }
    }

    private async void ProcessAvatarQueue(object state)
    {
        if (_disposed || _isProcessingQueue) return;

        if (!await _sceneLock.WaitAsync(TimeSpan.FromMilliseconds(100)))
        {
            return;
        }

        try
        {
            if (_isProcessingQueue) return;
            _isProcessingQueue = true;

            List<Func<Task<bool>>> tasksToProcess;
            lock (_pendingAvatarQueue)
            {
                tasksToProcess = _pendingAvatarQueue.ToList();
                _pendingAvatarQueue.Clear();
            }

            foreach (var taskFunc in tasksToProcess)
            {
                if (_cts.IsCancellationRequested) break;

                // Проверяем, готова ли сцена
                if (!_isSceneReady)
                {
                    _logger.LogDebug("WebViewService: Scene not ready, re-queueing...");
                    lock (_pendingAvatarQueue)
                    {
                        _pendingAvatarQueue.Enqueue(taskFunc);
                    }
                    break;
                }

                try
                {
                    await taskFunc();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "WebViewService: Queue task error");
                }

                // Пауза между аватарами для плавности
                await Task.Delay(150, _cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("ProcessAvatarQueue cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ProcessAvatarQueue error");
        }
        finally
        {
            _isProcessingQueue = false;
            _sceneLock.Release();
        }
    }

    private void HandleAvatarClicked(JsonElement root)
    {
        try
        {
            if (!root.TryGetProperty("userId", out var userIdElement))
            {
                _logger.LogWarning("HandleAvatarClicked: Missing userId");
                return;
            }

            var userId = userIdElement.GetString();
            if (string.IsNullOrEmpty(userId))
            {
                _logger.LogWarning("HandleAvatarClicked: Empty userId");
                return;
            }

            // Проверяем формат userId (GUID)
            if (!Guid.TryParse(userId, out _))
            {
                _logger.LogWarning($"HandleAvatarClicked: Invalid userId format: {userId}");
                return;
            }

            _logger.LogInformation($"👆 WebView: Avatar clicked {userId}");
            OnAvatarClicked?.Invoke(this, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HandleAvatarClicked error");
        }
    }

    private string EscapeJsString(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        return input
            .Replace("\\", "\\\\")
            .Replace("'", "\\'")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(WebViewService));
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("WebViewService: Disposing...");

        // Отменяем все операции
        _cts.Cancel();

        // Останавливаем таймер
        _queueTimer?.Dispose();

        // Очищаем ресурсы
        Cleanup();

        // Освобождаем семафоры
        _avatarLock?.Dispose();
        _sceneLock?.Dispose();
        _cts?.Dispose();

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("WebViewService: Disposed");
    }

    #endregion

    #region Вспомогательные классы

    public class AvatarInfo
    {
        public string userId { get; set; } = "";
        public string nickname { get; set; } = "";
        public string gender { get; set; } = "";
        public bool isLoaded { get; set; }
    }

    #endregion
}