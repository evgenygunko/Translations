// Ignore Spelling: Validator

using FluentValidation;

namespace TranslationsFunc.Models.Input
{
    public class MeaningValidator : AbstractValidator<Meaning>
    {
        public MeaningValidator()
        {
            RuleFor(model => model.id).GreaterThan(0)
                .WithMessage("Each meaning id must be an integer greater than 0."); ;

            RuleFor(model => model.Text).NotNull();

            RuleFor(model => model.Examples).NotNull();
        }
    }
}
