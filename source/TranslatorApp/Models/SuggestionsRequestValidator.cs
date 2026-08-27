using FluentValidation;
using FluentValidation.Results;

namespace TranslatorApp.Models
{
    public class SuggestionsRequestValidator : AbstractValidator<SuggestionsRequest>
    {
        public SuggestionsRequestValidator()
        {
            RuleFor(model => model.Text).NotEmpty();
            RuleFor(model => model.DestinationLanguage)
                .IsEnumName(typeof(SourceLanguage), caseSensitive: false)
                .WithMessage($"'DestinationLanguage' must be one of the following: {string.Join(", ", Enum.GetNames(typeof(SourceLanguage)))}");
        }

        protected override bool PreValidate(ValidationContext<SuggestionsRequest> context, ValidationResult result)
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
