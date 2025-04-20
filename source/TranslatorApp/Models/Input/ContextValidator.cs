// Ignore Spelling: Validator

using FluentValidation;

namespace TranslatorApp.Models.Input
{
    public class ContextValidator : AbstractValidator<ContextInput>
    {
        public ContextValidator()
        {
            RuleFor(model => model.id).GreaterThan(0);

            RuleFor(model => model.Meanings).NotNull();
            RuleForEach(model => model.Meanings)
                .SetValidator(new MeaningValidator());
        }
    }
}
