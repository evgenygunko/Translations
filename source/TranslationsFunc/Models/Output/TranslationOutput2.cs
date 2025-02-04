namespace TranslationsFunc.Models.Output
{
    public record TranslationOutput2(Headword[] Headword, Meaning[] Meanings);

    public record Headword(string Language, IEnumerable<string> TranslationVariants);

    public record Meaning(int id, TranslatedMeaning[] TranslatedMeanings);

    public record TranslatedMeaning(string Language, string Text);
}
