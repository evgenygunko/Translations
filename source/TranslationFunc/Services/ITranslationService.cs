using TranslationsFunc.Models;

namespace TranslationsFunc.Services
{
    public interface ITranslationService
    {
        Task<List<TranslationOutput>> TranslateAsync(TranslationInput translationInput);
    }
}
