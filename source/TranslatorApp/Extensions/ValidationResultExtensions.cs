// Ignore Spelling: Validator

using FluentValidation.Results;

namespace TranslatorApp.Extensions
{
    public static class ValidationResultExtensions
    {
        /// <summary>
        /// Formats validation errors into a single error message string.
        /// </summary>
        /// <param name="validation">The validation result containing errors.</param>
        /// <returns>A formatted error message string.</returns>
        public static string FormatErrorMessage(this ValidationResult validation)
        {
            string errorMessage = "Error: ";
            foreach (var failure in validation.Errors)
            {
                errorMessage += failure.ErrorMessage.TrimEnd('.') + ", ";
            }
            errorMessage = errorMessage.TrimEnd(',', ' ') + '.';

            return errorMessage;
        }
    }
}
