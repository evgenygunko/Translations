﻿// Ignore Spelling: Validator

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
            RuleForEach(o => o.Definitions)
                .SetValidator(new DefinitionValidator());

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
