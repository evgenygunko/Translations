namespace TranslatorApp.Models
{
    public interface IGlobalSettings
    {
        string RequestSecretCode { get; set; }

        string OpenAIApiKey { get; set; }

        string? BetterStackToken { get; set; }

        string? BetterStackIngestingHost { get; set; }

        bool? UseOpenAIResponseAPI { get; set; }

        string? LaunchDarklySdkKey { get; set; }
    }

    public class GlobalSettings : IGlobalSettings
    {
        public string RequestSecretCode { get; set; } = null!;
        public string OpenAIApiKey { get; set; } = null!;
        public string? BetterStackToken { get; set; }
        public string? BetterStackIngestingHost { get; set; }
        public bool? UseOpenAIResponseAPI { get; set; }
        public string? LaunchDarklySdkKey { get; set; }
    }
}
