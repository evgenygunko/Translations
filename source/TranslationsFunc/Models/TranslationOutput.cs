﻿namespace TranslationsFunc.Models
{
    public record TranslationOutput(TranslationItem[] Translations);

    public record TranslationItem(string Language, IEnumerable<string> TranslationVariants);
}
