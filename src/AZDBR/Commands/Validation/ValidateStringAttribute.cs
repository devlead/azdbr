namespace AZDBR.Commands.Validation;

/// <summary>
/// Validates that a command argument is a non-empty string with minimum length.
/// </summary>
public class ValidateStringAttribute : ParameterValidationAttribute
{
    /// <summary>
    /// Minimum allowed string length.
    /// </summary>
    public const int MinimumLength = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidateStringAttribute"/> class.
    /// </summary>
#nullable disable
    public ValidateStringAttribute()
        : base(errorMessage: null)
    {
    }
#nullable enable

    /// <inheritdoc />
    public override ValidationResult Validate(CommandParameterContext context)
    {
        if (context.Value is not string stringValue || string.IsNullOrWhiteSpace(stringValue))
        {
            return ValidationResult.Error($"Invalid {context.Parameter.PropertyName} specified.");
        }

        if (stringValue.Trim().Length < MinimumLength)
        {
            return ValidationResult.Error(
                $"{context.Parameter.PropertyName} must be at least {MinimumLength} character(s) long.");
        }

        try
        {
            Services.SqlIdentifierValidator.ToBracketedIdentifier(stringValue.Trim());
        }
        catch (ArgumentException exception)
        {
            return ValidationResult.Error(exception.Message);
        }

        return ValidationResult.Success();
    }
}
