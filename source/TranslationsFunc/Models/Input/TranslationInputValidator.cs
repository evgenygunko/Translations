﻿// Ignore Spelling: Validator

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

            RuleFor(x => x.Word).NotEmpty()
                .When(x => !HasEitherWordOrMeaning(x))
                .WithMessage("'Word' or 'Meaning' must not be empty.");
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

        private bool HasEitherWordOrMeaning(TranslationInput model)
        {
            return !string.IsNullOrEmpty(model.Word)
                || !string.IsNullOrEmpty(model.Meaning);
        }
    }
}
