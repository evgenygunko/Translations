// Ignore Spelling: Validator

using FluentValidation;
using FluentValidation.Results;

namespace TranslatorApp.Models
{
    public class NormalizeSoundRequestValidator : AbstractValidator<NormalizeSoundRequest>
    {
        public NormalizeSoundRequestValidator()
        {
            RuleFor(model => model.SoundUrl).NotEmpty()
                .WithMessage("'SoundUrl' must not be empty");

            RuleFor(model => model.SoundUrl)
                .Must(BeAValidUrl)
                .WithMessage("'SoundUrl' must be a valid URL");

            RuleFor(model => model.Word).NotEmpty();
        }

        protected override bool PreValidate(ValidationContext<NormalizeSoundRequest> context, ValidationResult result)
        {
            if (context.InstanceToValidate == null)
            {
                result.Errors.Add(new ValidationFailure("", "Please ensure a model was supplied."));
                return false;
            }
            return true;
        }

        private static bool BeAValidUrl(string url)
        {
            return Uri.TryCreate(url, UriKind.Absolute, out var uriResult)
                     && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
