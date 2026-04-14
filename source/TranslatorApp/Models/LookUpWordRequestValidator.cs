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
            RuleFor(model => model.ActiveDictionaries)
                .NotNull()
                .WithMessage("'ActiveDictionaries' must not be null.");
            RuleFor(model => model.ActiveDictionaries)
                .NotEmpty()
                .When(model => model.ActiveDictionaries != null)
                .WithMessage("'ActiveDictionaries' must not be empty.");
            RuleFor(model => model.ActiveDictionaries)
                .Must((model, activeDictionaries) =>
                    activeDictionaries != null
                    && activeDictionaries.Any(dictionary => string.Equals(dictionary, model.SourceLanguage, StringComparison.OrdinalIgnoreCase)))
                .When(model =>
                    model.ActiveDictionaries != null
                    && model.ActiveDictionaries.Count > 0
                    && Enum.TryParse<SourceLanguage>(model.SourceLanguage, ignoreCase: true, out _))
                .WithMessage("'ActiveDictionaries' must contain the value from 'SourceLanguage'.");
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
