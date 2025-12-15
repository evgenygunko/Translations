// Ignore Spelling: App

namespace TranslatorApp.Models
{
    public enum TranslatorAppEventId
    {
        WillTranslateWithOpenAI = 32,
        LookupRequestReceived = 35,
        WordNotFound = 36,
        ReturningWordModel = 37,
        TranslationReceived = 38,
        OpenAPIDidNotReturnContext = 39,
        LanguageSpecificCharactersFound = 40,
        RemoveAtPrefix = 41,
        ErrorDuringLookup = 42,
        NoTextFromOpenAI = 43,
        DownloadingSoundFile = 44,
        ExtractingAudioFromMP4 = 45,
        WroteInputFile = 46,
        AudioExtractionSuccessful = 47,
        AudioExtractionFailed = 48,
        SoundDownloadRequestReceived = 49,
        ErrorDownloadingSound = 50,
    }
}
