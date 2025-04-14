// Ignore Spelling: Validator

using FluentValidation;

namespace TranslationsFunc.Models.Input
{
    public class ContextValidator : AbstractValidator<Context>
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
