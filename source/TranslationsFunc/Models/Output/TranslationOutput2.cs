namespace TranslationsFunc.Models.Output
{
    // The output model returned by the Azure function.
    public record TranslationOutput2(
        DefinitionOutput2[] Definitions);

    public record DefinitionOutput2(
        int id,
        string HeadwordTranslation,
        string HeadwordTranslationEnglish,
        ContextOutput2[] Contexts);

    public record ContextOutput2(
        int id,
        MeaningOutput2[] Meanings);

    public record MeaningOutput2(
        int id,
        string MeaningTranslation);
}
