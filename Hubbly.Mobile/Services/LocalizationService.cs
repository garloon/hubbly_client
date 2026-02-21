using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Globalization;

namespace Hubbly.Mobile.Services;

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    event EventHandler<string> LanguageChanged;
    string GetString(string key);
    void SetLanguage(string languageCode);
    Dictionary<string, string> GetAllStrings();
}

public class LocalizationService : ILocalizationService, IDisposable
{
    private readonly ILogger<LocalizationService> _logger;
    private readonly string _languageKey = "app_language";
    private string _currentLanguage;
    private bool _disposed;

    private readonly Dictionary<string, Dictionary<string, string>> _localizations;

    public event EventHandler<string> LanguageChanged;

    public string CurrentLanguage => _currentLanguage;

    public LocalizationService(ILogger<LocalizationService> logger)
    {
        _logger = logger;

        // Initialize localization dictionaries
        _localizations = new Dictionary<string, Dictionary<string, string>>
        {
            ["ru"] = new()
            {
                // Common
                ["app_name"] = "Hubbly",
                ["ok"] = "OK",
                ["cancel"] = "Отмена",
                ["save"] = "Сохранить",
                ["loading"] = "Загрузка...",
                ["error"] = "Ошибка",
                ["retry"] = "Повторить",

                // Welcome Page
                ["welcome_title"] = "Добро пожаловать в",
                ["welcome_subtitle"] = "Социальный хаб",
                ["welcome_description"] = "Общайтесь в реальном времени",
                ["welcome_button"] = "Войти как гость",
                ["guest_mode_info"] = "Никакой регистрации • Ваш гостевой ID создастся автоматически",
                ["terms_of_service"] = "Условиями использования",
                ["privacy_policy"] = "Конфиденциальностью",
                ["agreement"] = "Продолжая, вы соглашаетесь с нашими",

                // Avatar Selection
                ["avatar_title"] = "ВЫБЕРИТЕ АВАТАРА",
                ["avatar_subtitle"] = "Пол и внешность вашего персонажа",
                ["male"] = "МУЖСКОЙ",
                ["female"] = "ЖЕНСКИЙ",
                ["enter_chat"] = "ВОЙТИ В ЧАТ",
                ["back"] = "Назад",

                // Chat Room
                ["chat_room"] = "Чат-комната",
                ["online"] = "Онлайн",
                ["type_message"] = "Напишите сообщение...",
                ["send"] = "➤",
                ["connection_lost"] = "Соединение потеряно. Переподключение...",
                ["server_unavailable"] = "Chat server is not responding. Please try again later.",

                // Settings
                ["settings_title"] = "Настройки",
                ["theme"] = "Тема",
                ["light_theme"] = "Светлая",
                ["dark_theme"] = "Тёмная",
                ["language"] = "Язык",
                ["russian"] = "Русский",
                ["english"] = "English",

                // About
                ["about_title"] = "О приложении",
                ["about_app_name"] = "Hubbly Social Hub",
                ["about_version"] = "Версия",
                ["about_description"] = "Социальный хаб для общения в реальном времени с 3D-аватарами.",
                ["terms"] = "Условия использования",
                ["privacy"] = "Политика конфиденциальности",

                // Logout
                ["logout_title"] = "Выход",
                ["logout_confirm"] = "Вы уверены, что хотите выйти?",
                ["logout_yes"] = "Да, выйти",
                ["logout_no"] = "Отмена",

                // Notifications
                ["notification_server_unavailable"] = "Сервер недоступен. Проверьте подключение к интернету.",
                ["notification_auth_failed"] = "Ошибка аутентификации. Попробуйте снова.",
                ["notification_network_error"] = "Ошибка сети. Проверьте подключение.",
            },
            ["en"] = new()
            {
                // Common
                ["app_name"] = "Hubbly",
                ["ok"] = "OK",
                ["cancel"] = "Cancel",
                ["save"] = "Save",
                ["loading"] = "Loading...",
                ["error"] = "Error",
                ["retry"] = "Retry",

                // Welcome Page
                ["welcome_title"] = "Welcome to",
                ["welcome_subtitle"] = "Social Hub",
                ["welcome_description"] = "Chat in real-time",
                ["welcome_button"] = "Enter as Guest",
                ["guest_mode_info"] = "No registration • Your guest ID will be created automatically",
                ["terms_of_service"] = "Terms of Service",
                ["privacy_policy"] = "Privacy Policy",
                ["agreement"] = "By continuing, you agree to our",

                // Avatar Selection
                ["avatar_title"] = "SELECT AVATAR",
                ["avatar_subtitle"] = "Choose your character's gender and appearance",
                ["male"] = "MALE",
                ["female"] = "FEMALE",
                ["enter_chat"] = "ENTER CHAT",
                ["back"] = "Back",

                // Chat Room
                ["chat_room"] = "Chat Room",
                ["online"] = "Online",
                ["type_message"] = "Type a message...",
                ["send"] = "➤",
                ["connection_lost"] = "Connection lost. Reconnecting...",
                ["server_unavailable"] = "Chat server is not responding. Please try again later.",

                // Settings
                ["settings_title"] = "Settings",
                ["theme"] = "Theme",
                ["light_theme"] = "Light",
                ["dark_theme"] = "Dark",
                ["language"] = "Language",
                ["russian"] = "Русский",
                ["english"] = "English",

                // About
                ["about_title"] = "About",
                ["about_app_name"] = "Hubbly Social Hub",
                ["about_version"] = "Version",
                ["about_description"] = "Real-time social hub with 3D avatars.",
                ["terms"] = "Terms of Service",
                ["privacy"] = "Privacy Policy",

                // Logout
                ["logout_title"] = "Logout",
                ["logout_confirm"] = "Are you sure you want to logout?",
                ["logout_yes"] = "Yes, logout",
                ["logout_no"] = "Cancel",

                // Notifications
                ["notification_server_unavailable"] = "Server unavailable. Check your internet connection.",
                ["notification_auth_failed"] = "Authentication failed. Please try again.",
                ["notification_network_error"] = "Network error. Check your connection.",
            }
        };

        // Load saved language
        var savedLanguage = Preferences.Get(_languageKey, "ru");
        _currentLanguage = savedLanguage == "en" ? "en" : "ru";

        _logger.LogInformation("LocalizationService initialized. Current language: {Language}", _currentLanguage);
    }

    public string GetString(string key)
    {
        if (_localizations.TryGetValue(_currentLanguage, out var dict) && dict.TryGetValue(key, out var value))
        {
            return value;
        }

        // Fallback to Russian
        if (_currentLanguage != "ru" && _localizations["ru"].TryGetValue(key, out var fallback))
        {
            return fallback;
        }

        // Return key if not found
        _logger.LogWarning("Localization key not found: {Key} for language: {Language}", key, _currentLanguage);
        return key;
    }

    public void SetLanguage(string languageCode)
    {
        if (_currentLanguage == languageCode)
            return;

        _currentLanguage = languageCode == "en" ? "en" : "ru";
        Preferences.Set(_languageKey, _currentLanguage);

        _logger.LogInformation("Language changed to: {Language}", _currentLanguage);
        LanguageChanged?.Invoke(this, _currentLanguage);
    }

    public Dictionary<string, string> GetAllStrings()
    {
        if (_localizations.TryGetValue(_currentLanguage, out var dict))
        {
            return new Dictionary<string, string>(dict);
        }

        return new Dictionary<string, string>();
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("LocalizationService disposing...");

        _disposed = true;
        GC.SuppressFinalize(this);

        _logger.LogInformation("LocalizationService disposed");
    }
}
