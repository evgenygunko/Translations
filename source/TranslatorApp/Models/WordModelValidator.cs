// Ignore Spelling: Validator

using FluentValidation;
using FluentValidation.Results;

namespace TranslatorApp.Models
{
    public class WordModelValidator : AbstractValidator<WordModel>
    {
        public WordModelValidator()
        {
            RuleFor(model => model.Word).NotEmpty();
            RuleFor(model => model.SourceLanguage).IsInEnum();
            RuleFor(model => model.Definition).NotNull();

            When(model => model.Definition != null, () =>
            {
                RuleFor(model => model.Definition.Headword).NotNull();
                RuleFor(model => model.Definition.Headword.Original)
                    .NotEmpty()
                    .When(model => model.Definition.Headword != null);
                RuleFor(model => model.Definition.Contexts).NotNull();
                RuleFor(model => model.Definition.Contexts)
                    .NotEmpty()
                    .When(model => model.Definition.Contexts != null);
                RuleForEach(model => model.Definition.Contexts)
                    .NotNull()
                    .ChildRules(context =>
                    {
                        context.RuleFor(value => value.Meanings).NotNull();
                        context.RuleForEach(value => value.Meanings)
                            .NotNull()
                            .ChildRules(meaning =>
                            {
                                meaning.RuleFor(value => value.Original).NotEmpty();
                                meaning.RuleFor(value => value.Examples).NotNull();
                            });
                    })
                    .When(model => model.Definition.Contexts != null);
            });

            RuleFor(model => model.Variants).NotNull();
            RuleFor(model => model.Expressions).NotNull();
        }

        protected override bool PreValidate(ValidationContext<WordModel> context, ValidationResult result)
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
