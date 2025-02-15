﻿namespace TranslationsFunc.Models.Input
{
    public record TranslationInput(
        string SourceLanguage,
        IEnumerable<string> DestinationLanguages,
        string Word,
        string Meaning,
        string PartOfSpeech,
        IEnumerable<string> Examples);
}
