namespace TranslatorApp.Models.Input.V1
{
    public record TranslationInput(
        string Text,
        string SourceLanguage,
        string DestinationLanguage,
        string Version);
}
