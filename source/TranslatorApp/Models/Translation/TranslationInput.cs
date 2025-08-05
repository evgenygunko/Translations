// Ignore Spelling: App

namespace TranslatorApp.Models.Translation
{
    public record TranslationInput(
        string Version,
        string SourceLanguage,
        string DestinationLanguage,
        IEnumerable<DefinitionInput> Definitions);

    public record DefinitionInput(
        int id,
        HeadwordInput Headword,
        IEnumerable<ContextInput> Contexts);

    public record HeadwordInput(
        string Text,
        string? PartOfSpeech,
        string Meaning,
        IEnumerable<string> Examples);

    public record ContextInput(
        int id,
        string? ContextString,
        IEnumerable<MeaningInput> Meanings);

    public record MeaningInput(
        int id,
        string Text,
        string? PartOfSpeech,
        IEnumerable<string> Examples);
}
