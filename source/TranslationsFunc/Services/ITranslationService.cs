using TranslationsFunc.Models.Input;
using TranslationsFunc.Models.Output;

namespace TranslationsFunc.Services
{
    public interface ITranslationService
    {
        Task<TranslationOutput> TranslateAsync(TranslationInput translationInput);
    }
}
