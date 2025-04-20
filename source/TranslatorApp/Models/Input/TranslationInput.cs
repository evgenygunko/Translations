namespace TranslatorApp.Models.Input
{
    public record TranslationInput(
        string Version,
        string SourceLanguage,
        string DestinationLanguage,
        IEnumerable<DefinitionInput> Definitions);

    public record DefinitionInput(
        int id,
        string? PartOfSpeech,
        HeadwordInput Headword,
        IEnumerable<ContextInput> Contexts);

    public record ContextInput(
        int id,
        string? ContextString,
        IEnumerable<MeaningInput> Meanings);

    public record HeadwordInput(
        string Text,
        string Meaning,
        IEnumerable<string> Examples);

    public record MeaningInput(
        int id,
        string Text,
        IEnumerable<string> Examples);
}
