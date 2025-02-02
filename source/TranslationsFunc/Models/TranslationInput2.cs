namespace TranslationsFunc.Models
{
    public record TranslationInput2(
        string Version,
        string SourceLanguage,
        IEnumerable<string> DestinationLanguages,
        string Word,
        Headword Headword,
        IEnumerable<Meaning> Meanings);

    public record Headword(
        string Meaning,
        string PartOfSpeech,
        IEnumerable<string> Examples);

    public record Meaning(
        int id,
        string Text,
        IEnumerable<string> Examples);
}
