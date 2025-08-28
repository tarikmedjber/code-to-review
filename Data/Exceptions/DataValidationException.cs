namespace MedjCap.Data.Exceptions;

/// <summary>
/// Exception thrown when data validation fails during analysis operations.
/// Provides detailed information about validation failures and remediation steps.
/// </summary>
public class DataValidationException : MedjCapException
{
    /// <summary>
    /// Field or property name that failed validation
    /// </summary>
    public string FieldName { get; }

    /// <summary>
    /// Value that failed validation
    /// </summary>
    public object? InvalidValue { get; }

    /// <summary>
    /// Type of validation that failed
    /// </summary>
    public DataValidationType ValidationType { get; }

    /// <summary>
    /// Expected valid range or criteria
    /// </summary>
    public string ValidationCriteria { get; }

    /// <summary>
    /// Additional validation failures if multiple fields are invalid
    /// </summary>
    public ValidationFailure[] AdditionalFailures { get; }

    public DataValidationException(
        string fieldName,
        object? invalidValue,
        DataValidationType validationType,
        string validationCriteria,
        ValidationFailure[]? additionalFailures = null,
        Exception? innerException = null)
        : base(
            errorCode: "DATA_VALIDATION_FAILED",
            message: $"Data validation failed for field '{fieldName}': {validationType}",
            userMessage: CreateUserMessage(fieldName, invalidValue, validationType, validationCriteria, additionalFailures),
            context: CreateContext(fieldName, invalidValue, validationType, validationCriteria, additionalFailures),
            innerException: innerException)
    {
        FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
        InvalidValue = invalidValue;
        ValidationType = validationType;
        ValidationCriteria = validationCriteria ?? throw new ArgumentNullException(nameof(validationCriteria));
        AdditionalFailures = additionalFailures ?? Array.Empty<ValidationFailure>();
    }

    /// <summary>
    /// Creates a validation exception for multiple field failures
    /// </summary>
    public static DataValidationException CreateMultipleFailures(
        ValidationFailure[] failures,
        string? operationContext = null)
    {
        if (!failures.Any())
            throw new ArgumentException("Must provide at least one validation failure", nameof(failures));

        var primary = failures.First();
        var additional = failures.Skip(1).ToArray();

        var context = new Dictionary<string, object>
        {
            ["TotalFailures"] = failures.Length,
            ["OperationContext"] = operationContext ?? "Unknown"
        };

        return new DataValidationException(
            primary.FieldName,
            primary.InvalidValue,
            primary.ValidationType,
            primary.ValidationCriteria,
            additional);
    }

    private static string CreateUserMessage(
        string fieldName,
        object? invalidValue,
        DataValidationType validationType,
        string criteria,
        ValidationFailure[]? additional)
    {
        var baseMessage = validationType switch
        {
            DataValidationType.Required => $"Field '{fieldName}' is required but was not provided.",
            DataValidationType.Range => $"Field '{fieldName}' value '{invalidValue}' is outside the valid range: {criteria}.",
            DataValidationType.Format => $"Field '{fieldName}' value '{invalidValue}' does not match required format: {criteria}.",
            DataValidationType.BusinessRule => $"Field '{fieldName}' violates business rule: {criteria}.",
            DataValidationType.Dependency => $"Field '{fieldName}' has invalid dependency relationship: {criteria}.",
            DataValidationType.Uniqueness => $"Field '{fieldName}' value '{invalidValue}' must be unique: {criteria}.",
            _ => $"Field '{fieldName}' failed validation: {criteria}."
        };

        if (additional?.Any() == true)
        {
            baseMessage += $" Additionally, {additional.Length} other field(s) also failed validation.";
        }

        return baseMessage + " Please correct the data and retry the operation.";
    }

    private static Dictionary<string, object> CreateContext(
        string fieldName,
        object? invalidValue,
        DataValidationType validationType,
        string criteria,
        ValidationFailure[]? additional)
    {
        var context = new Dictionary<string, object>
        {
            ["FieldName"] = fieldName,
            ["InvalidValue"] = invalidValue?.ToString() ?? "null",
            ["ValidationType"] = validationType.ToString(),
            ["ValidationCriteria"] = criteria,
            ["InvalidValueType"] = invalidValue?.GetType().Name ?? "null",
            ["AdditionalFailureCount"] = additional?.Length ?? 0
        };

        if (additional?.Any() == true)
        {
            context["AdditionalFailures"] = additional.Select(f => new
            {
                Field = f.FieldName,
                Type = f.ValidationType.ToString(),
                Value = f.InvalidValue?.ToString() ?? "null"
            }).ToArray();
        }

        return context;
    }

    /// <summary>
    /// Gets all validation failures including the primary failure
    /// </summary>
    public ValidationFailure[] GetAllFailures()
    {
        var primary = new ValidationFailure(FieldName, InvalidValue, ValidationType, ValidationCriteria);
        return new[] { primary }.Concat(AdditionalFailures).ToArray();
    }
}

/// <summary>
/// Represents a single validation failure
/// </summary>
public record ValidationFailure(
    string FieldName,
    object? InvalidValue,
    DataValidationType ValidationType,
    string ValidationCriteria);

/// <summary>
/// Types of data validation that can fail
/// </summary>
public enum DataValidationType
{
    Required,
    Range,
    Format,
    BusinessRule,
    Dependency,
    Uniqueness,
    CustomValidation
}