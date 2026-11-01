using System.Windows;

namespace MeridianClient.Services;

public interface ILocalizationService
{
    string CurrentLanguage { get; }
    void SetLanguage(string languageCode);
    string GetString(string key);
    event Action? LanguageChanged;
}

public class LocalizationService : ILocalizationService
{
    public static LocalizationService Instance { get; } = new();

    private string _currentLanguage = "ru";
    public string CurrentLanguage => _currentLanguage;

    public event Action? LanguageChanged;

    private static readonly Dictionary<string, string> RuDictionary = new()
    {
        // App / Shell
        ["App_Title"] = "Meridian Search Platform — Cyber OSINT",
        ["Header_CyberOsint"] = "CYBER OSINT",
        ["Header_Platform"] = "Meridian Search Platform",
        ["Branding_Subtitle"] = "Advanced Reconnaissance Intelligence",
        ["Nav_Search"] = "Поиск целей",
        ["Nav_Graph"] = "Граф связей",
        ["Nav_History"] = "История сессий",
        ["Nav_Logs"] = "Журнал логов",
        ["Nav_Settings"] = "Настройки",
        ["Health_BackendOnline"] = "Бэкенд доступен",
        ["Health_Connecting"] = "Проверка шлюза...",
        ["Health_Offline"] = "Бэкенд недоступен",
        ["Health_Tooltip"] = "Нажмите для проверки соединения с бэкендом",

        // Search View
        ["Search_TargetPlaceholder"] = "Цель разведки (домен, IP-адрес: example.com)",
        ["Search_StartButton"] = "Начать поиск",
        ["Search_StopButton"] = "Остановить",
        ["Search_ClearButton"] = "Очистить",
        ["Search_ReadyStatus"] = "Готов к запуску поиска",
        ["Search_FilterAll"] = "Все находки",
        ["Search_FilterDNS"] = "DNS записи",
        ["Search_FilterWhois"] = "WHOIS данные",
        ["Search_FilterSubdomains"] = "Поддомены",
        ["Search_FilterTech"] = "Веб-стек",
        ["Search_FilterOther"] = "Прочее",
        ["Search_FilterPlaceholder"] = "Фильтрация находок (IP, хост, тип, провайдер)...",
        ["Search_EmptyTitle"] = "Ожидание поисковой сессии",
        ["Search_EmptyDesc"] = "Укажите целевой домен или IP-адрес и активируйте поиск для потокового приёма разведданных в реальном времени.",
        ["Search_Card_Ttl"] = "TTL: {0}s",
        ["Search_Card_Priority"] = "PRIO: {0}",
        ["Search_Card_Registrar"] = "РЕГИСТРАТОР:",
        ["Search_Card_Created"] = "СОЗДАН",
        ["Search_Card_Expires"] = "ИСТЕКАЕТ",
        ["Search_Card_Country"] = "СТРАНА",
        ["Search_Card_Status"] = "СТАТУС",
        ["Search_Card_CopyJson"] = "Копировать JSON",
        ["Search_Card_CopySubdomain"] = "Копировать поддомен",
        ["Search_Card_CopyUrl"] = "Копировать URL",
        ["Search_Card_Issuer"] = "ИЗДАТЕЛЬ:",
        ["Search_Card_ValidFrom"] = "ОТ:",
        ["Search_Card_ValidTo"] = "ДО:",
        ["Search_Card_Server"] = "СЕРВЕР:",
        ["Search_Card_Framework"] = "СТЕК:",
        ["Search_Metric_Findings"] = "НАХОДКИ:",
        ["Search_Metric_DNS"] = "DNS:",
        ["Search_Metric_WHOIS"] = "WHOIS:",
        ["Search_Metric_Subdomains"] = "ПОДДОМЕНЫ:",
        ["Search_Metric_Tech"] = "ВЕБ-СТЕК:",

        // History View
        ["History_Title"] = "История поисковых сессий",
        ["History_Subtitle"] = "Сохранённые сессии разведки в хранилище PostgreSQL",
        ["History_RefreshBtn"] = "Обновить список",
        ["History_OpenSession"] = "Открыть сессию",
        ["History_EmptyTitle"] = "История сессий пуста",
        ["History_EmptyDesc"] = "Нажмите «Обновить список» для загрузки сохраненных сессий с сервера Meridian Search Platform.",
        ["History_Card_Created"] = "СОЗДАНА:",
        ["History_Card_Finished"] = "ЗАВЕРШЕНА:",
        ["History_Card_Findings"] = "НАХОДОК:",

        // Logs View
        ["Logs_Title"] = "Системный журнал событий и телеметрии",
        ["Logs_Subtitle"] = "Поток событий Cyber OSINT, вызовов REST API, gRPC streaming и Health Check",
        ["Logs_Level_All"] = "Все",
        ["Logs_Level_Info"] = "Инфо",
        ["Logs_Level_Warning"] = "Варнинги",
        ["Logs_Level_Error"] = "Ошибки",
        ["Logs_Level_Debug"] = "Отладка",
        ["Logs_FilterPlaceholder"] = "Фильтр по тексту, коду, URL...",
        ["Logs_AutoScroll"] = "Автопрокрутка",
        ["Logs_CopyAll"] = "Копировать всё",
        ["Logs_Clear"] = "Очистить логи",
        ["Logs_EmptyTitle"] = "Журнал логов чист",
        ["Logs_EmptyDesc"] = "События REST API, gRPC и Health Check появятся здесь при выполнении операций.",

        // Settings View
        ["Settings_Title"] = "Конфигурация и персонализация",
        ["Settings_Subtitle"] = "Сетевые параметры шлюза, оформление и локализация интерфейса",
        ["Settings_Network_Header"] = "СЕТЕВЫЕ ПАРАМЕТРЫ ШЛЮЗА:",
        ["Settings_BackendUrl"] = "БАЗОВЫЙ URL БЭКЕНДА:",
        ["Settings_AllowHttp2"] = "Разрешить незашифрованный HTTP/2 (SocketsHttpHandler.Http2UnencryptedSupport)",
        ["Settings_Timeout"] = "ТАЙМАУТ СЕТЕВЫХ ЗАПРОСОВ (СЕК):",
        ["Settings_TestBtn"] = "Проверить соединение",
        ["Settings_Appearance_Header"] = "ОФОРМЛЕНИЕ И ЯЗЫК:",
        ["Settings_Theme_Preset"] = "ПРЕСЕТ ТЕМЫ:",
        ["Settings_Theme_Custom"] = "Кастомная тема",
        ["Settings_Theme_BgColor"] = "ЦВЕТ ФОНА (HEX):",
        ["Settings_Theme_CardColor"] = "ЦВЕТ КАРТОЧЕК (HEX):",
        ["Settings_Theme_FgColor"] = "ЦВЕТ ТЕКСТА (HEX):",
        ["Settings_Theme_AccentColor"] = "АКЦЕНТНЫЙ ЦВЕТ (HEX):",
        ["Settings_Language_Label"] = "ЯЗЫК ИНТЕРФЕЙСА:",
        ["Settings_Language_Ru"] = "Русский (RU)",
        ["Settings_Language_En"] = "English (EN)",
        ["Settings_DiagHeader"] = "ДИАГНОСТИКА СОЕДИНЕНИЯ:"
    };

    private static readonly Dictionary<string, string> EnDictionary = new()
    {
        // App / Shell
        ["App_Title"] = "Meridian Search Platform — Cyber OSINT",
        ["Header_CyberOsint"] = "CYBER OSINT",
        ["Header_Platform"] = "Meridian Search Platform",
        ["Branding_Subtitle"] = "Advanced Reconnaissance Intelligence",
        ["Nav_Search"] = "Target Search",
        ["Nav_Graph"] = "Relation Graph",
        ["Nav_History"] = "Session History",
        ["Nav_Logs"] = "Event Logs",
        ["Nav_Settings"] = "Settings",
        ["Health_BackendOnline"] = "Backend Online",
        ["Health_Connecting"] = "Checking gateway...",
        ["Health_Offline"] = "Backend Offline",
        ["Health_Tooltip"] = "Click to ping backend server",

        // Search View
        ["Search_TargetPlaceholder"] = "Reconnaissance target (domain, IP: example.com)",
        ["Search_StartButton"] = "Start Search",
        ["Search_StopButton"] = "Stop Stream",
        ["Search_ClearButton"] = "Clear Feed",
        ["Search_ReadyStatus"] = "Ready to launch search",
        ["Search_FilterAll"] = "All findings",
        ["Search_FilterDNS"] = "DNS records",
        ["Search_FilterWhois"] = "WHOIS data",
        ["Search_FilterSubdomains"] = "Subdomains",
        ["Search_FilterTech"] = "Web Tech",
        ["Search_FilterOther"] = "Other",
        ["Search_FilterPlaceholder"] = "Filter findings (IP, host, type, provider)...",
        ["Search_EmptyTitle"] = "Awaiting Search Session",
        ["Search_EmptyDesc"] = "Specify a target domain or IP address and start search to stream reconnaissance cards in real-time.",
        ["Search_Card_Ttl"] = "TTL: {0}s",
        ["Search_Card_Priority"] = "PRIO: {0}",
        ["Search_Card_Registrar"] = "REGISTRAR:",
        ["Search_Card_Created"] = "CREATED",
        ["Search_Card_Expires"] = "EXPIRES",
        ["Search_Card_Country"] = "COUNTRY",
        ["Search_Card_Status"] = "STATUS",
        ["Search_Card_CopyJson"] = "Copy JSON",
        ["Search_Card_CopySubdomain"] = "Copy Subdomain",
        ["Search_Card_CopyUrl"] = "Copy URL",
        ["Search_Card_Issuer"] = "ISSUER:",
        ["Search_Card_ValidFrom"] = "FROM:",
        ["Search_Card_ValidTo"] = "TO:",
        ["Search_Card_Server"] = "SERVER:",
        ["Search_Card_Framework"] = "STACK:",
        ["Search_Metric_Findings"] = "FINDINGS:",
        ["Search_Metric_DNS"] = "DNS:",
        ["Search_Metric_WHOIS"] = "WHOIS:",
        ["Search_Metric_Subdomains"] = "SUBDOMAINS:",
        ["Search_Metric_Tech"] = "WEB TECH:",

        // History View
        ["History_Title"] = "Reconnaissance Session History",
        ["History_Subtitle"] = "Persisted OSINT sessions retrieved from PostgreSQL storage",
        ["History_RefreshBtn"] = "Refresh List",
        ["History_OpenSession"] = "Open Session",
        ["History_EmptyTitle"] = "Session History is Empty",
        ["History_EmptyDesc"] = "Click 'Refresh List' to fetch stored sessions from the Meridian Host backend.",
        ["History_Card_Created"] = "CREATED:",
        ["History_Card_Finished"] = "FINISHED:",
        ["History_Card_Findings"] = "FINDINGS:",

        // Logs View
        ["Logs_Title"] = "System Event & Telemetry Console",
        ["Logs_Subtitle"] = "Live event stream of REST API calls, gRPC streams, and health check heartbeats",
        ["Logs_Level_All"] = "All",
        ["Logs_Level_Info"] = "Info",
        ["Logs_Level_Warning"] = "Warnings",
        ["Logs_Level_Error"] = "Errors",
        ["Logs_Level_Debug"] = "Debug",
        ["Logs_FilterPlaceholder"] = "Filter by text, code, URL...",
        ["Logs_AutoScroll"] = "Auto-scroll",
        ["Logs_CopyAll"] = "Copy All",
        ["Logs_Clear"] = "Clear Logs",
        ["Logs_EmptyTitle"] = "Event log is clear",
        ["Logs_EmptyDesc"] = "REST API, gRPC streaming, and health check diagnostics will appear here live.",

        // Settings View
        ["Settings_Title"] = "Configuration & Personalization",
        ["Settings_Subtitle"] = "Backend gateway parameters, appearance theme, and interface language",
        ["Settings_Network_Header"] = "BACKEND GATEWAY PARAMETERS:",
        ["Settings_BackendUrl"] = "BACKEND BASE URL:",
        ["Settings_AllowHttp2"] = "Allow unencrypted HTTP/2 (SocketsHttpHandler.Http2UnencryptedSupport)",
        ["Settings_Timeout"] = "REQUEST TIMEOUT (SECONDS):",
        ["Settings_TestBtn"] = "Test Connection",
        ["Settings_Appearance_Header"] = "APPEARANCE & LOCALIZATION:",
        ["Settings_Theme_Preset"] = "THEME PRESET:",
        ["Settings_Theme_Custom"] = "Custom Theme",
        ["Settings_Theme_BgColor"] = "BACKGROUND COLOR (HEX):",
        ["Settings_Theme_CardColor"] = "CARD COLOR (HEX):",
        ["Settings_Theme_FgColor"] = "FOREGROUND COLOR (HEX):",
        ["Settings_Theme_AccentColor"] = "ACCENT COLOR (HEX):",
        ["Settings_Language_Label"] = "LANGUAGE:",
        ["Settings_Language_Ru"] = "Русский (RU)",
        ["Settings_Language_En"] = "English (EN)",
        ["Settings_DiagHeader"] = "CONNECTION DIAGNOSTICS:"
    };

    public void SetLanguage(string languageCode)
    {
        _currentLanguage = languageCode.Equals("en", StringComparison.OrdinalIgnoreCase) ? "en" : "ru";
        ApplyResources();
        LanguageChanged?.Invoke();
    }

    public string GetString(string key)
    {
        var dict = _currentLanguage == "en" ? EnDictionary : RuDictionary;
        if (dict.TryGetValue(key, out var val))
            return val;
        if (RuDictionary.TryGetValue(key, out var fallback))
            return fallback;
        return key;
    }

    private void ApplyResources()
    {
        var dict = _currentLanguage == "en" ? EnDictionary : RuDictionary;
        if (Application.Current?.Resources == null) return;

        foreach (var (k, v) in dict)
        {
            Application.Current.Resources[k] = v;
        }
    }
}
