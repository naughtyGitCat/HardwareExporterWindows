using System.Globalization;
using System.Text.Json;

namespace HardwareExporterWeb.Services;

public class LocalizationService
{
    private readonly ILogger<LocalizationService> _logger;
    private Dictionary<string, string> _localizations = new();
    private CultureInfo _currentCulture = CultureInfo.CurrentCulture;

    public event Action? OnLanguageChanged;

    public LocalizationService(ILogger<LocalizationService> _logger)
    {
        this._logger = _logger;
        LoadLocalization(_currentCulture.Name);
    }

    public string this[string key]
    {
        get
        {
            if (_localizations.TryGetValue(key, out var value))
            {
                return value;
            }
            _logger.LogWarning("Localization key not found: {Key}", key);
            return key;
        }
    }

    public string GetString(string key, params object[] args)
    {
        var format = this[key];
        try
        {
            return string.Format(format, args);
        }
        catch
        {
            return format;
        }
    }

    public CultureInfo CurrentCulture => _currentCulture;

    public void SetLanguage(string cultureName)
    {
        var culture = new CultureInfo(cultureName);
        _currentCulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        
        LoadLocalization(cultureName);
        OnLanguageChanged?.Invoke();
    }

    private void LoadLocalization(string cultureName)
    {
        // Map culture names to file names
        var fileName = cultureName switch
        {
            "zh-CN" or "zh" or "zh-Hans" => "Localization.zh-CN.json",
            "ja" or "ja-JP" => "Localization.ja.json",
            _ => "Localization.en.json"
        };

        var filePath = Path.Combine(AppContext.BaseDirectory, "Resources", fileName);
        
        try
        {
            if (File.Exists(filePath))
            {
                var json = File.ReadAllText(filePath);
                _localizations = JsonSerializer.Deserialize<Dictionary<string, string>>(json) 
                    ?? new Dictionary<string, string>();
                _logger.LogInformation("Loaded localization: {FileName}", fileName);
            }
            else
            {
                _logger.LogWarning("Localization file not found: {FilePath}", filePath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load localization file: {FilePath}", filePath);
        }
    }

    public List<(string Code, string Name)> GetAvailableLanguages()
    {
        return new List<(string Code, string Name)>
        {
            ("en", "English"),
            ("zh-CN", "简体中文"),
            ("ja", "日本語")
        };
    }
}
