namespace TranslatorApp.Models
{
    public record SuggestionsRequest(
        string Text,
        string DestinationLanguage);
}
