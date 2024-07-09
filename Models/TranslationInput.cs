namespace TranslationsFunc.Models
{
    public record TranslationInput(string HeadWord, string Meaning, string SourceLanguage, IEnumerable<string> DestinationLanguages);
}
