namespace TranslationsFunc.Models.Output
{
    // The output model returned by the Azure function.
    public record TranslationOutput2(DefinitionTranslations[] Definitions);

    public record DefinitionTranslations(
        int id,
        Headword[] Headword,
        Context[] Contexts);

    public record Context(
        int id,
        Meaning[] Meanings);
}
