using Azure.AI.Translation.Text;
using FluentAssertions;
using TranslationsFunc.Models.Input;
using TranslationsFunc.Services;

namespace TranslationsFunc.Tests.Services
{
    [TestClass]
    public class AzureTranslationServiceTests
    {
        [TestMethod]
        public void CreateTranslationOptions_WhenWordIsNotNull_WillTranslateWord()
        {
            IEnumerable<string> examples = ["example 1", "example 2"];
            TranslationInput input = new TranslationInput(SourceLanguage: "da", ["en", "ru"], Word: "Word", Meaning: "", PartOfSpeech: "", examples);

            TextTranslationTranslateOptions result = AzureTranslationService.CreateTranslationOptions(input);
            result.Content.FirstOrDefault().Should().Be("Word");
        }

        [TestMethod]
        public void CreateTranslationOptions_WhenWordIsNullAndMeaningIsNot_WillTranslateMeaning()
        {
            IEnumerable<string> examples = ["example 1", "example 2"];
            TranslationInput input = new TranslationInput("da", ["en", "ru"], Word: "", Meaning: "Meaning", PartOfSpeech: "", examples);

            TextTranslationTranslateOptions result = AzureTranslationService.CreateTranslationOptions(input);
            result.Content.FirstOrDefault().Should().Be("Meaning");
        }
    }
}
