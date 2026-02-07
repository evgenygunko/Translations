// Ignore Spelling: Validator

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
            RuleFor(model => model.DestinationLanguage)
                .Must((model, destinationLanguage) => !string.Equals(destinationLanguage, model.SourceLanguage, StringComparison.OrdinalIgnoreCase))
                .WithMessage("'DestinationLanguage' must not be equal to 'SourceLanguage'.");
            RuleFor(model => model.Text).NotEmpty();
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
    }
}
