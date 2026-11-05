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
        ["Nav_Welcome"] = "Главная",
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
        ["Search_FilterEmail"] = "Email / Профили",
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
        ["Search_Metric_Email"] = "EMAIL ДАННЫЕ:",

        ["Search_Status_EnterTarget"] = "Введите целевой домен или IP",
        ["Search_Status_Initializing"] = "Инициализация сессии через REST API...",
        ["Search_Status_Connecting"] = "Сессия создана [{0}]. Подключение к gRPC стриму...",
        ["Search_Status_Launched"] = "Поиск запущен для цели: {0}",
        ["Search_Status_Completed"] = "Сессия завершена. Всего находок: {0}",
        ["Search_Status_Stopped"] = "Поиск остановлен пользователем.",

        ["Search_Card_Disposable_BurnerTitle"] = "ВРЕМЕННАЯ ОДНОРАЗОВАЯ ПОЧТА (BURNER)",
        ["Search_Card_Disposable_LegitTitle"] = "ЛЕГИТИМНЫЙ ПОЧТОВЫЙ ДОМЕН",
        ["Search_Card_Disposable_BurnerMsg"] = "Внимание: почтовый ящик или домен зарегистрирован в базе сервисов временной почты. Высокий риск фейка, мошенничества или спам-активности.",
        ["Search_Card_Disposable_LegitMsg"] = "Признаков временного или одноразового ящика не обнаружено. Стандартный почтовый провайдер.",
        ["Search_Card_Disposable_Domain"] = "ДОМЕН ПОЧТЫ",
        ["Search_Card_Disposable_Category"] = "КАТЕГОРИЯ",
        ["Search_Card_Disposable_TargetEmail"] = "ЦЕЛЕВОЙ EMAIL",

        ["Search_Card_Mx_Domain"] = "ПОЧТОВЫЙ ДОМЕН:",
        ["Search_Card_Mx_Gateways"] = "ПОЧТОВЫЕ ШЛЮЗЫ (MX SERVERS):",
        ["Search_Card_Mx_SpfActive"] = "SPF ЗАЩИТА АКТИВНА",
        ["Search_Card_Mx_SpfMissing"] = "НЕТ SPF ЗАЩИТЫ",

        ["Search_Card_Profile_Url"] = "ПРОФИЛЬ:",
        ["Search_Card_Profile_Btn"] = "Профиль",
        ["Avatar_Dialog_Title_GitHub"] = "Профиль GitHub",
        ["Avatar_Dialog_Title_Gravatar"] = "Профиль Gravatar",
        ["Avatar_Dialog_Title_Telegram"] = "Профиль Telegram",
        ["Avatar_Dialog_OpenProfile"] = "Открыть профиль",
        ["Avatar_Dialog_Close"] = "Закрыть",

        // Graph View Toasts & Alerts
        ["Graph_Toast_PivotErrorTitle"] = "Ошибка Pivot-исследования",
        ["Graph_Toast_InvalidTarget"] = "Неверное значение для поиска: «{0}». Узел не содержит корректного домена, IP-адреса или имени пользователя.",
        ["Graph_Toast_BadRequest"] = "Сервер отклонил цель «{0}»: неверный формат значения (400 Bad Request).",
        ["Graph_Toast_GenericError"] = "Ошибка при запуске исследования «{0}»: {1}",
        ["Graph_AutoExpand_Off"] = "Авто: ВЫКЛ",
        ["Graph_AutoExpand_On"] = "Авто: Smart",
        ["Graph_Btn_ExpandNode"] = "⚡ Развернуть узел (Pivot)",
        ["Graph_Btn_NodeExpanded"] = "✔ Узел уже развёрнут",
        ["Graph_Btn_NodeExpanding"] = "⏳ Разворачивание...",
        ["Graph_Toast_AlreadyVisited"] = "Цель «{0}» уже была исследована. Связи объединены на графе.",
        ["Graph_Toast_LimitTitle"] = "Лимит холста",
        ["Graph_Toast_LimitReached"] = "Достигнут лимит узлов на холсте (150). Развёртывание остановлено для предотвращения перегрузки графа.",
        ["Graph_Toast_AutoExpandStarted"] = "Smart Auto-Expand запущен для {0} ключевых находок (1 уровень глубины).",
        ["Graph_Btn_SmartLayout_ToolTip"] = "Организовать и зафиксировать (умная раскладка + стоп-физика)",
        ["Graph_Toast_SmartLayoutTitle"] = "Раскладка зафиксирована",
        ["Graph_Toast_SmartLayoutSuccess"] = "Иерархическая структура выстроена (Meridian Smart Tree). Физика остановлена.",

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
        ["Settings_Notifications_Label"] = "ВСПЛЫВАЮЩИЕ УВЕДОМЛЕНИЯ:",
        ["Settings_Notifications_Title"] = "Всплывающие Toast-уведомления",
        ["Settings_Notifications_Subtitle"] = "Отображать всплывающие уведомления о системных событиях, поиске и графе в нижнем правом углу",
        ["Settings_Graph_Label"] = "ПАРАМЕТРЫ СВЯЗЕЙ ГРАФА:",
        ["Settings_Graph_MergeShared_Title"] = "Объединять общие сущности в графе",
        ["Settings_Graph_MergeShared_Subtitle"] = "Связывать одинаковые узлы (общие IP, юзернеймы, почты) в единый узел-мост между разными ветками поиска",
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
        ["Nav_Welcome"] = "Home",
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
        ["Search_FilterEmail"] = "Email / Profiles",
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
        ["Search_Metric_Email"] = "EMAIL OSINT:",

        ["Search_Status_EnterTarget"] = "Please enter target domain or IP",
        ["Search_Status_Initializing"] = "Initializing session via REST API...",
        ["Search_Status_Connecting"] = "Session created [{0}]. Connecting to gRPC stream...",
        ["Search_Status_Launched"] = "Search launched for target: {0}",
        ["Search_Status_Completed"] = "Session completed. Total findings: {0}",
        ["Search_Status_Stopped"] = "Search stopped by user.",

        ["Search_Card_Disposable_BurnerTitle"] = "TEMPORARY DISPOSABLE MAIL (BURNER)",
        ["Search_Card_Disposable_LegitTitle"] = "LEGITIMATE MAIL DOMAIN",
        ["Search_Card_Disposable_BurnerMsg"] = "Warning: Mailbox or domain is registered with disposable email services. High risk of burner, fraudulent or spam activity.",
        ["Search_Card_Disposable_LegitMsg"] = "No temporary or burner mailbox detected. Standard email provider.",
        ["Search_Card_Disposable_Domain"] = "MAIL DOMAIN",
        ["Search_Card_Disposable_Category"] = "CATEGORY",
        ["Search_Card_Disposable_TargetEmail"] = "TARGET EMAIL",

        ["Search_Card_Mx_Domain"] = "EMAIL DOMAIN:",
        ["Search_Card_Mx_Gateways"] = "MAIL EXCHANGERS (MX SERVERS):",
        ["Search_Card_Mx_SpfActive"] = "SPF PROTECTED",
        ["Search_Card_Mx_SpfMissing"] = "NO SPF PROTECTION",

        ["Search_Card_Profile_Url"] = "PROFILE:",
        ["Search_Card_Profile_Btn"] = "Profile",
        ["Avatar_Dialog_Title_GitHub"] = "GitHub Profile",
        ["Avatar_Dialog_Title_Gravatar"] = "Gravatar Profile",
        ["Avatar_Dialog_Title_Telegram"] = "Telegram Profile",
        ["Avatar_Dialog_OpenProfile"] = "Open Profile",
        ["Avatar_Dialog_Close"] = "Close",

        // Graph View Toasts & Alerts
        ["Graph_Toast_PivotErrorTitle"] = "Pivot Investigation Error",
        ["Graph_Toast_InvalidTarget"] = "Invalid search target: '{0}'. Node does not contain a valid domain, IP address, or username.",
        ["Graph_Toast_BadRequest"] = "The server rejected target '{0}': invalid value format (400 Bad Request).",
        ["Graph_Toast_GenericError"] = "Error starting investigation for '{0}': {1}",
        ["Graph_AutoExpand_Off"] = "Auto: OFF",
        ["Graph_AutoExpand_On"] = "Auto: Smart",
        ["Graph_Btn_ExpandNode"] = "⚡ Expand Node (Pivot)",
        ["Graph_Btn_NodeExpanded"] = "✔ Node Already Expanded",
        ["Graph_Btn_NodeExpanding"] = "⏳ Expanding...",
        ["Graph_Toast_AlreadyVisited"] = "Target '{0}' has already been investigated. Connections merged on graph.",
        ["Graph_Toast_LimitTitle"] = "Canvas Limit",
        ["Graph_Toast_LimitReached"] = "Graph canvas limit reached (150 nodes). Expansion halted to prevent graph explosion.",
        ["Graph_Toast_AutoExpandStarted"] = "Smart Auto-Expand launched for {0} priority findings (Depth 1).",
        ["Graph_Btn_SmartLayout_ToolTip"] = "Organize and Lock (smart layout + stop physics)",
        ["Graph_Toast_SmartLayoutTitle"] = "Layout Locked",
        ["Graph_Toast_SmartLayoutSuccess"] = "Hierarchical structure arranged (Meridian Smart Tree). Physics stopped.",

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
        ["Settings_Notifications_Label"] = "POPUP NOTIFICATIONS:",
        ["Settings_Notifications_Title"] = "Popup Toast Notifications",
        ["Settings_Notifications_Subtitle"] = "Display popup notifications for system events, reconnaissance and graph in bottom-right corner",
        ["Settings_Graph_Label"] = "RELATION GRAPH SETTINGS:",
        ["Settings_Graph_MergeShared_Title"] = "Merge shared graph entities",
        ["Settings_Graph_MergeShared_Subtitle"] = "Link identical entities (shared IPs, usernames, emails) into a single bridge node between different search targets",
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
