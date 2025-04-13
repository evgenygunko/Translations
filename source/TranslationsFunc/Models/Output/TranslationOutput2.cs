namespace TranslationsFunc.Models.Output
{
    // The output model returned by the Azure function.
    public record TranslationOutput2(DefinitionTranslations[] Definitions);

    public record DefinitionTranslations(
        int id,
        Headword[] Headword,
        Meaning[] Meanings);
}
