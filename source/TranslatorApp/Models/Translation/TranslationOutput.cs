// Ignore Spelling: App

namespace TranslatorApp.Models.Translation
{
    // The output model returned by the ASP.NET web application.
    public record TranslationOutput(
        DefinitionOutput Definition);

    public record DefinitionOutput(
        string HeadwordTranslation,
        string HeadwordTranslationEnglish,
        ContextOutput[] Contexts);

    public record ContextOutput(
        int id,
        MeaningOutput[] Meanings);

    public record MeaningOutput(
        int id,
        string MeaningTranslation);
}
