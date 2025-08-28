namespace MedjCap.Data.Exceptions;

/// <summary>
/// Exception thrown when configuration validation fails or configuration is missing/invalid.
/// Provides guidance on proper configuration setup and validation.
/// </summary>
public class ConfigurationException : MedjCapException
{
    /// <summary>
    /// Name of the configuration section or property that failed
    /// </summary>
    public string ConfigurationKey { get; }

    /// <summary>
    /// Invalid configuration value
    /// </summary>
    public object? InvalidValue { get; }

    /// <summary>
    /// Expected value or valid range
    /// </summary>
    public string ExpectedValue { get; }

    /// <summary>
    /// Configuration file or source where the issue was found
    /// </summary>
    public string ConfigurationSource { get; }

    public ConfigurationException(
        string configurationKey,
        object? invalidValue,
        string expectedValue,
        string configurationSource = "Unknown",
        string? additionalGuidance = null,
        Exception? innerException = null)
        : base(
            errorCode: "INVALID_CONFIGURATION",
            message: $"Invalid configuration for '{configurationKey}': expected {expectedValue}, got {invalidValue}",
            userMessage: CreateUserMessage(configurationKey, invalidValue, expectedValue, additionalGuidance),
            context: CreateContext(configurationKey, invalidValue, expectedValue, configurationSource, additionalGuidance),
            innerException: innerException)
    {
        ConfigurationKey = configurationKey ?? throw new ArgumentNullException(nameof(configurationKey));
        InvalidValue = invalidValue;
        ExpectedValue = expectedValue ?? throw new ArgumentNullException(nameof(expectedValue));
        ConfigurationSource = configurationSource;
    }

    private static string CreateUserMessage(string key, object? value, string expected, string? guidance)
    {
        var baseMessage = $"Configuration error in '{key}': expected {expected}, but got '{value}'.";
        
        if (!string.IsNullOrWhiteSpace(guidance))
        {
            baseMessage += $" {guidance}";
        }

        return baseMessage + " Please update your configuration and restart the application.";
    }

    private static Dictionary<string, object> CreateContext(
        string key, 
        object? value, 
        string expected,
        string source,
        string? guidance)
    {
        return new Dictionary<string, object>
        {
            ["ConfigurationKey"] = key,
            ["InvalidValue"] = value?.ToString() ?? "null",
            ["ExpectedValue"] = expected,
            ["ConfigurationSource"] = source,
            ["ValueType"] = value?.GetType().Name ?? "null",
            ["AdditionalGuidance"] = guidance ?? "None",
            ["ConfigurationPath"] = key.Contains(':') ? key.Split(':').First() : "Root"
        };
    }

    /// <summary>
    /// Creates a configuration exception for missing required configuration
    /// </summary>
    public static ConfigurationException CreateMissingConfiguration(
        string configurationKey,
        string defaultValue,
        string configurationSource = "appsettings.json")
    {
        return new ConfigurationException(
            configurationKey,
            null,
            $"non-null value (suggested default: {defaultValue})",
            configurationSource,
            $"Add '{configurationKey}: {defaultValue}' to your {configurationSource} file.");
    }

    /// <summary>
    /// Creates a configuration exception for invalid range values
    /// </summary>
    public static ConfigurationException CreateRangeError(
        string configurationKey,
        object invalidValue,
        double minValue,
        double maxValue,
        string configurationSource = "appsettings.json")
    {
        return new ConfigurationException(
            configurationKey,
            invalidValue,
            $"value between {minValue} and {maxValue}",
            configurationSource,
            $"Update '{configurationKey}' to be within the valid range [{minValue}, {maxValue}].");
    }

    /// <summary>
    /// Gets configuration fix suggestions based on the key and error type
    /// </summary>
    public string[] GetFixSuggestions()
    {
        var suggestions = new List<string>();

        if (InvalidValue == null)
        {
            suggestions.Add($"Add '{ConfigurationKey}' to your configuration file");
            suggestions.Add($"Ensure the configuration section is properly structured");
        }
        else
        {
            suggestions.Add($"Update '{ConfigurationKey}' to match expected format: {ExpectedValue}");
            suggestions.Add("Check for typos in configuration keys and values");
        }

        // Add key-specific suggestions
        if (ConfigurationKey.Contains("Statistical"))
        {
            suggestions.Add("Refer to StatisticalConfig documentation for valid ranges");
            suggestions.Add("Statistical thresholds should typically be between 0 and 1");
        }
        else if (ConfigurationKey.Contains("Optimization"))
        {
            suggestions.Add("Refer to OptimizationConfig documentation for parameter limits");
            suggestions.Add("Iteration counts should be positive integers");
        }
        else if (ConfigurationKey.Contains("Validation"))
        {
            suggestions.Add("Refer to ValidationConfig documentation for valid settings");
            suggestions.Add("Window sizes and fold counts must be positive");
        }

        suggestions.Add("Restart the application after making configuration changes");
        
        return suggestions.ToArray();
    }
}