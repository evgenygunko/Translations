// Ignore Spelling: Validator

using AutoFixture;
using FluentAssertions;
using FluentValidation.Results;
using TranslatorApp.Models.Input;

namespace TranslatorApp.Tests.Models
{
    [TestClass]
    public class TranslationInput2ValidatorTests
    {
        private readonly IFixture _fixture = FixtureFactory.Create();

        [TestMethod]
        public void Validate_WhenAllRequiredFieldsHaveValues_ReturnsTrue()
        {
            TranslationInput translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Definitions: [
                    new DefinitionInput(
                        id: 1,
                        PartOfSpeech: "PartOfSpeech",
                        Headword: new HeadwordInput("word", "Meaning", Examples: []),
                        Contexts: [
                            new ContextInput(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [new MeaningInput(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]
                            )
                        ]
                    )
                ]);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenSourceLanguageIsEmpty_ReturnsFalse()
        {
            TranslationInput translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "",
                DestinationLanguage: "ru",
                Definitions: [
                    new DefinitionInput(
                        id: 1,
                        PartOfSpeech: "PartOfSpeech",
                        Headword: new HeadwordInput("word", "Meaning", Examples: []),
                        Contexts: [
                            new ContextInput(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [ new MeaningInput(id : 1, Text : "meaning 1", Examples :["Meaning 1, example 1"])]
                            )
                        ]
                    )
                ]);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Source Language' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenDestinationLanguageIsNull_ReturnsFalse()
        {
            TranslationInput translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "da",
                DestinationLanguage: null!,
                Definitions: [
                    new DefinitionInput(
                        id: 1,
                        PartOfSpeech: "PartOfSpeech",
                        Headword: new HeadwordInput("word", "Meaning", Examples: []),
                        Contexts: [
                            new ContextInput(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [ new MeaningInput(id : 1, Text : "meaning 1", Examples :["Meaning 1, example 1"]) ]
                            )
                        ]
                    )
                ]);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Destination Language' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenIdIsLessThanOne_ReturnsFalse()
        {
            TranslationInput translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Definitions: [
                    new DefinitionInput(
                        id: 0,
                        PartOfSpeech: "PartOfSpeech",
                        Headword: default!,
                        Contexts :[
                            new ContextInput(
                                id : 1,
                                ContextString: "context 1",
                                Meanings: [ new MeaningInput(id : 1, Text : "meaning 1", Examples :["Meaning 1, example 1"]) ]
                            )
                        ]
                    )
                ]);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(2);
            result.Errors[0].ErrorMessage.Should().Be("'id' must be greater than '0'.");
            result.Errors[1].ErrorMessage.Should().Be("Each definition must have a Headword.");
        }

        [TestMethod]
        public void Validate_WhenHeadwordIsNull_ReturnsFalse()
        {
            TranslationInput translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Definitions: [
                    new DefinitionInput(
                        id: 1,
                        PartOfSpeech: "PartOfSpeech",
                        Headword: default!,
                        Contexts: [
                            new ContextInput(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [ new MeaningInput(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"]) ]
                            )
                        ]
                    )
                ]);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("Each definition must have a Headword.");
        }

        [TestMethod]
        public void Validate_WhenHeadwordTextIsEmpty_ReturnsFalse()
        {
            TranslationInput translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Definitions: [
                    new DefinitionInput(
                        id: 1,
                        PartOfSpeech: "PartOfSpeech",
                        Headword: new HeadwordInput("", "Meaning", Examples: []),
                        Contexts: [
                            new ContextInput(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [ new MeaningInput(id : 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"]) ]
                            )
                        ]
                    )
                ]);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Headword Text' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenMeaningsAreEmpty_ReturnsTrue()
        {
            TranslationInput translationInput = new TranslationInput(
                Version: "2",
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Definitions: [
                    new DefinitionInput(
                        id: 1,
                        PartOfSpeech: "PartOfSpeech",
                        Headword: new HeadwordInput("word", "Meaning", Examples: []),
                        Contexts: [
                            new ContextInput(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: []
                            )
                        ]
                    )
                ]);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeTrue();
        }

        [DataTestMethod]
        [DataRow("1")]
        [DataRow("3")]
        public void Validate_WhenVersionDoesNotEqual2_ReturnsFalse(string version)
        {
            TranslationInput translationInput = new TranslationInput(
                Version: version,
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Definitions: [
                    new DefinitionInput(
                        id: 1,
                        PartOfSpeech: "PartOfSpeech",
                        Headword: new HeadwordInput("word", "Meaning", Examples: []),
                        Contexts: [
                            new ContextInput(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [ new MeaningInput(id : 1, Text : "meaning 1", Examples :["Meaning 1, example 1"]) ]
                            )
                        ]
                    )
                ]);

            var sut = _fixture.Create<TranslationInputValidator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
        }
    }
}
