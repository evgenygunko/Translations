// Ignore Spelling: Validator

using AutoFixture;
using FluentAssertions;
using FluentValidation.Results;
using TranslationsFunc.Models.Input;

namespace TranslationFunc.Tests.Models
{
    [TestClass]
    public class TranslationInput2ValidatorTests
    {
        private readonly Fixture _fixture = FixtureFactory.CreateFixture();

        [TestMethod]
        public void Validate_WhenAllRequiredFieldsHaveValues_ReturnsTrue()
        {
            TranslationInput2 translationInput = new TranslationInput2(
                Version: "2",
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Definitions: [
                    new Definition2(
                        id: 1,
                        PartOfSpeech: "PartOfSpeech",
                        Headword: new Headword2("word", "Meaning", Examples: []),
                        Contexts: [
                            new Context2(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"])]
                            )
                        ]
                    )
                ]);

            var sut = _fixture.Create<TranslationInput2Validator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenSourceLanguageIsEmpty_ReturnsFalse()
        {
            TranslationInput2 translationInput = new TranslationInput2(
                Version: "2",
                SourceLanguage: "",
                DestinationLanguage: "ru",
                Definitions: [
                    new Definition2(
                        id: 1,
                        PartOfSpeech: "PartOfSpeech",
                        Headword: new Headword2("word", "Meaning", Examples: []),
                        Contexts: [
                            new Context2(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [ new Meaning(id : 1, Text : "meaning 1", Examples :["Meaning 1, example 1"])]
                            )
                        ]
                    )
                ]);

            var sut = _fixture.Create<TranslationInput2Validator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Source Language' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenDestinationLanguageIsNull_ReturnsFalse()
        {
            TranslationInput2 translationInput = new TranslationInput2(
                Version: "2",
                SourceLanguage: "da",
                DestinationLanguage: null!,
                Definitions: [
                    new Definition2(
                        id: 1,
                        PartOfSpeech: "PartOfSpeech",
                        Headword: new Headword2("word", "Meaning", Examples: []),
                        Contexts: [
                            new Context2(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [ new Meaning(id : 1, Text : "meaning 1", Examples :["Meaning 1, example 1"]) ]
                            )
                        ]
                    )
                ]);

            var sut = _fixture.Create<TranslationInput2Validator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Destination Language' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenIdIsLessThanOne_ReturnsFalse()
        {
            TranslationInput2 translationInput = new TranslationInput2(
                Version: "2",
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Definitions: [
                    new Definition2(
                        id: 0,
                        PartOfSpeech: "PartOfSpeech",
                        Headword: default!,
                        Contexts :[
                            new Context2(
                                id : 1,
                                ContextString: "context 1",
                                Meanings: [ new Meaning(id : 1, Text : "meaning 1", Examples :["Meaning 1, example 1"]) ]
                            )
                        ]
                    )
                ]);

            var sut = _fixture.Create<TranslationInput2Validator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(2);
            result.Errors[0].ErrorMessage.Should().Be("'id' must be greater than '0'.");
            result.Errors[1].ErrorMessage.Should().Be("Each definition must have a Headword.");
        }

        [TestMethod]
        public void Validate_WhenHeadwordIsNull_ReturnsFalse()
        {
            TranslationInput2 translationInput = new TranslationInput2(
                Version: "2",
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Definitions: [
                    new Definition2(
                        id: 1,
                        PartOfSpeech: "PartOfSpeech",
                        Headword: default!,
                        Contexts: [
                            new Context2(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [ new Meaning(id: 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"]) ]
                            )
                        ]
                    )
                ]);

            var sut = _fixture.Create<TranslationInput2Validator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("Each definition must have a Headword.");
        }

        [TestMethod]
        public void Validate_WhenHeadwordTextIsEmpty_ReturnsFalse()
        {
            TranslationInput2 translationInput = new TranslationInput2(
                Version: "2",
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Definitions: [
                    new Definition2(
                        id: 1,
                        PartOfSpeech: "PartOfSpeech",
                        Headword: new Headword2("", "Meaning", Examples: []),
                        Contexts: [
                            new Context2(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [ new Meaning(id : 1, Text: "meaning 1", Examples: ["Meaning 1, example 1"]) ]
                            )
                        ]
                    )
                ]);

            var sut = _fixture.Create<TranslationInput2Validator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Headword Text' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenMeaningsAreEmpty_ReturnsTrue()
        {
            TranslationInput2 translationInput = new TranslationInput2(
                Version: "2",
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Definitions: [
                    new Definition2(
                        id: 1,
                        PartOfSpeech: "PartOfSpeech",
                        Headword: new Headword2("word", "Meaning", Examples: []),
                        Contexts: [
                            new Context2(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: []
                            )
                        ]
                    )
                ]);

            var sut = _fixture.Create<TranslationInput2Validator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeTrue();
        }

        [DataTestMethod]
        [DataRow("1")]
        [DataRow("3")]
        public void Validate_WhenVersionDoesNotEqual2_ReturnsFalse(string version)
        {
            TranslationInput2 translationInput = new TranslationInput2(
                Version: version,
                SourceLanguage: "da",
                DestinationLanguage: "ru",
                Definitions: [
                    new Definition2(
                        id: 1,
                        PartOfSpeech: "PartOfSpeech",
                        Headword: new Headword2("word", "Meaning", Examples: []),
                        Contexts: [
                            new Context2(
                                id: 1,
                                ContextString: "context 1",
                                Meanings: [ new Meaning(id : 1, Text : "meaning 1", Examples :["Meaning 1, example 1"]) ]
                            )
                        ]
                    )
                ]);

            var sut = _fixture.Create<TranslationInput2Validator>();
            ValidationResult result = sut.Validate(translationInput);

            result.IsValid.Should().BeFalse();
        }
    }
}
