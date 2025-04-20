// Ignore Spelling: Validator

using FluentValidation;

namespace TranslatorApp.Models.Input
{
    public class MeaningValidator : AbstractValidator<MeaningInput>
    {
        public MeaningValidator()
        {
            RuleFor(model => model.id).GreaterThan(0);

            RuleFor(model => model.Text).NotNull();

            RuleFor(model => model.Examples).NotNull();
        }
    }
}
