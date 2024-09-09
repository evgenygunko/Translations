namespace TranslationsFunc.Models
{
    public record TranslationInput(string HeadWord, IEnumerable<string> Meanings, string SourceLanguage, IEnumerable<string> DestinationLanguages);
}
