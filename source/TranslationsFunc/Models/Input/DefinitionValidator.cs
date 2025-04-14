// Ignore Spelling: Validator

using FluentValidation;

namespace TranslationsFunc.Models.Input
{
    public class DefinitionValidator : AbstractValidator<Definition>
    {
        public DefinitionValidator()
        {
            RuleFor(model => model.id).GreaterThan(0)
                .WithMessage("Each definition id must be an integer greater than 0."); ;

            RuleFor(model => model.Headword).NotNull()
                .WithMessage("Each definition must have a Headword.");
            RuleFor(model => model.Headword.Text).NotEmpty().When(d => d.Headword != null);

            RuleFor(model => model.Meanings).NotNull();
            RuleForEach(model => model.Meanings)
                .SetValidator(new MeaningValidator());
        }
    }
}
