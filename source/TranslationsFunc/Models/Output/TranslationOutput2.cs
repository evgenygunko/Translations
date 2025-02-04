namespace TranslationsFunc.Models.Output
{
    // The output model returned by the Azure function.
    public record TranslationOutput2(Headword[] Headword, Meaning[] Meanings);

    public record Headword(string Language, IEnumerable<string> HeadwordTranslations);

    public record Meaning(int id, MeaningTranslation[] MeaningTranslations);

    public record MeaningTranslation(string Language, string Text);

    // The model returned by OpenAI's API.
    public record OpenAIHeadwordTranslations(Headword[] Translations);

    // The model returned by OpenAI's API.
    public record OpenAIMeaningsTranslations(Meaning[] Translations);
}
