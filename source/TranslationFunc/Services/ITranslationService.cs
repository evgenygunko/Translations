using TranslationsFunc.Models;

namespace TranslationsFunc.Services
{
    public interface ITranslationService
    {
        Task<TranslationOutput> TranslateAsync(TranslationInput translationInput);
    }
}
