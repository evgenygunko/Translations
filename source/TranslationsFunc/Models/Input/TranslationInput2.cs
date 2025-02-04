﻿namespace TranslationsFunc.Models.Input
{
    public record TranslationInput2(
        string Version,
        string SourceLanguage,
        IEnumerable<string> DestinationLanguages,
        Headword Headword,
        IEnumerable<Meaning> Meanings);

    public record Headword(
        string Text,
        string Meaning,
        string PartOfSpeech,
        IEnumerable<string> Examples);

    public record Meaning(
        int id,
        string Text,
        IEnumerable<string> Examples);
}
