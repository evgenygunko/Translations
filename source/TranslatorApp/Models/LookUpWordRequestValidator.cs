﻿// Ignore Spelling: Validator

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
            RuleFor(model => model.Text).NotEmpty();
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
    }
}
