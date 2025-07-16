// Ignore Spelling: Validator

using FluentValidation;
using FluentValidation.Results;

namespace TranslatorApp.Models.Input.V1
{
    public class TranslationInputValidator : AbstractValidator<TranslationInput>
    {
        public TranslationInputValidator()
        {
            RuleFor(model => model.SourceLanguage).NotEmpty();

            RuleFor(model => model.DestinationLanguage).NotEmpty();

            RuleFor(model => model.Text).NotEmpty();

            RuleFor(model => model.Version).Equal("1");
        }

        protected override bool PreValidate(ValidationContext<TranslationInput> context, ValidationResult result)
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
