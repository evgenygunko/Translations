namespace TranslationsFunc.Models.Input
{
    public record TranslationInput2(
        string Version,
        string SourceLanguage,
        string DestinationLanguage,
        IEnumerable<Definition2> Definitions);

    public record Definition2(
        int id,
        string PartOfSpeech,
        Headword2 Headword,
        IEnumerable<Context2> Contexts);

    public record Context2(
        int id,
        string ContextString,
        IEnumerable<Meaning> Meanings);

    public record Headword2(
        string Text,
        string Meaning,
        IEnumerable<string> Examples);

    public record Meaning2(
        int id,
        string Text,
        IEnumerable<string> Examples);
}
