// Ignore Spelling: Validator

using System.Text.RegularExpressions; // Add this if not already present
using CopyWords.Parsers;
using CopyWords.Parsers.Models;
using FluentValidation;
using FluentValidation.Results;

namespace TranslatorApp.Models
{
    public class LookUpWordRequestValidator : AbstractValidator<LookUpWordRequest>
    {
        public LookUpWordRequestValidator()
        {
            RuleFor(x => x.SourceLanguage).IsEnumName(typeof(SourceLanguage), caseSensitive: false)
                .WithMessage($"'SourceLanguage' must be one of the following: {string.Join(", ", Enum.GetNames(typeof(SourceLanguage)))}");

            RuleFor(model => model.DestinationLanguage).NotEmpty();

            RuleFor(model => model.Text)
                .NotEmpty()
                .Must(text => ValidateDDOUri(text) || Regex.IsMatch(text, @"^[\w ]+$"))
                .WithMessage($"'Text' can only contain alphanumeric characters and spaces.");

            RuleFor(model => model.Version).Equal("1");
        }

        protected override bool PreValidate(ValidationContext<LookUpWordRequest> context, ValidationResult result)
        {
            if (context.InstanceToValidate == null)
            {
                result.Errors.Add(new ValidationFailure("", "Please ensure a model was supplied."));
                return false;
            }
            return true;
        }

        public bool ValidateDDOUri(string text)
        {
            // just so the validation passes if the uri is not required / nullable
            if (string.IsNullOrEmpty(text))
            {
                return true;
            }

            Uri? uri;
            if (Uri.TryCreate(text, UriKind.Absolute, out uri) && uri != null)
            {
                var leftPart = uri.GetLeftPart(UriPartial.Path);
                if (string.Equals(leftPart, DDOPageParser.DDOBaseUrl, StringComparison.CurrentCultureIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
