﻿// Ignore Spelling: Validator

using FluentValidation;

namespace TranslatorApp.Models.Input
{
    public class DefinitionValidator : AbstractValidator<DefinitionInput>
    {
        public DefinitionValidator()
        {
            RuleFor(model => model.id).GreaterThan(0);

            RuleFor(model => model.Headword).NotNull()
                .WithMessage("Each definition must have a Headword.");
            RuleFor(model => model.Headword.Text).NotEmpty().When(d => d.Headword != null);

            RuleFor(model => model.Contexts).NotNull();
            RuleForEach(model => model.Contexts)
                .SetValidator(new ContextValidator());
        }
    }
}
