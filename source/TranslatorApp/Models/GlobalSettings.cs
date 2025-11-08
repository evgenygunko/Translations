namespace TranslatorApp.Models
{
    public interface IGlobalSettings
    {
        string RequestSecretCode { get; set; }
    }

    public class GlobalSettings : IGlobalSettings
    {
        public string RequestSecretCode { get; set; } = null!;
    }
}
