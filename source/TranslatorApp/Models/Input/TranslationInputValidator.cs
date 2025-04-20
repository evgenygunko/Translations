// Ignore Spelling: Validator

using FluentValidation;
using FluentValidation.Results;

namespace TranslatorApp.Models.Input
{
    public class TranslationInputValidator : AbstractValidator<TranslationInput>
    {
        public TranslationInputValidator()
        {
            RuleFor(model => model.SourceLanguage).NotEmpty();

            RuleFor(model => model.DestinationLanguage).NotEmpty();

            RuleFor(model => model.Definitions).NotNull();
            RuleForEach(o => o.Definitions)
                .SetValidator(new DefinitionValidator());

            RuleFor(model => model.Version).Equal("2");
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
