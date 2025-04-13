// Ignore Spelling: Validator

using FluentValidation;
using FluentValidation.Results;

namespace TranslationsFunc.Models.Input
{
    public class TranslationInput2Validator : AbstractValidator<TranslationInput2>
    {
        public TranslationInput2Validator()
        {
            RuleFor(model => model.SourceLanguage).NotEmpty();

            RuleFor(model => model.DestinationLanguages)
                .NotEmpty()
                .WithMessage("'DestinationLanguages' must have at least one element and fewer than two.");
            RuleFor(model => model.DestinationLanguages)
                .Must(collection => collection == null || collection.Count() > 0 && collection.Count() <= 2)
                .WithMessage("'DestinationLanguages' must have at least one element and fewer than two.");

            RuleFor(model => model.Definitions).NotNull();

            RuleForEach(model => model.Definitions).ChildRules(definition =>
            {
                definition.RuleFor(d => d.id).GreaterThan(0)
                    .WithMessage("Each definition id must be an integer greater than 0."); ;

                definition.RuleFor(d => d.Headword).NotNull()
                    .WithMessage("Each definition must have a Headword.");
                definition.RuleFor(d => d.Headword.Text).NotEmpty().When(d => d.Headword != null);

                definition.RuleFor(d => d.Meanings)
                    .Must(collection => collection != null && collection.Count() > 0)
                    .WithMessage("Each definition must have at least one Meaning.");
            });

            RuleFor(model => model.Version).Equal("2");
        }

        protected override bool PreValidate(ValidationContext<TranslationInput2> context, ValidationResult result)
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
