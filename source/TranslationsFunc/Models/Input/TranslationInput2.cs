namespace TranslationsFunc.Models.Input
{
    public record TranslationInput2(
        string Version,
        string SourceLanguage,
        IEnumerable<string> DestinationLanguages,
        IEnumerable<Definition> Definitions);

    public record Definition(
        int id,
        Headword Headword,
        IEnumerable<Meaning> Meanings);
}
