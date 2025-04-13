// Ignore Spelling: Validator

using FluentValidation;
using FluentValidation.Results;

namespace TranslationsFunc.Models.Input
{
    public class TranslationInputValidator : AbstractValidator<TranslationInput>
    {
        public TranslationInputValidator()
        {
            RuleFor(model => model.SourceLanguage).NotEmpty();

            RuleFor(model => model.DestinationLanguages)
                .NotEmpty()
                .WithMessage("'DestinationLanguages' must have at least one element and fewer than two.");
            RuleFor(model => model.DestinationLanguages)
                .Must(collection => collection == null || collection.Count() > 0 && collection.Count() <= 2)
                .WithMessage("'DestinationLanguages' must have at least one element and fewer than two.");

            RuleFor(model => model.Headword).NotNull();
            RuleFor(model => model.Headword.Text)
               .NotEmpty()
               .When(model => model.Headword != null);

            RuleFor(model => model.Meanings)
                .Must(collection => collection == null || collection.Count() > 0)
                .WithMessage("'Meanings' must have at least one element.");

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
