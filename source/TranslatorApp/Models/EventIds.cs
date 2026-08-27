// Ignore Spelling: App

namespace TranslatorApp.Models
{
    public enum TranslatorAppEventId
    {
        WillTranslateWithOpenAI = 32,
        TranslationReceived = 38,
        OpenAPIDidNotReturnContext = 39,
        ErrorDuringLookup = 42,
        NoTextFromOpenAI = 43,
        DownloadingSoundFile = 44,
        ExtractingAudioFromMP4 = 45,
        WroteInputFile = 46,
        AudioExtractionSuccessful = 47,
        AudioExtractionFailed = 48,
        SoundDownloadRequestReceived = 49,
        ErrorDownloadingSound = 50,
        CallingOpenAITimeoudOut = 52,
        DownloadingSoundTimedOut = 53,
        ReturningWordModel2 = 54,
        TranslationSuggestionsReceived = 57,
    }
}
