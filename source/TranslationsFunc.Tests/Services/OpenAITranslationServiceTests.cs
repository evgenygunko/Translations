﻿using AutoFixture;
using FluentAssertions;
using TranslationFunc.Tests;
using TranslationsFunc.Models;
using TranslationsFunc.Services;

namespace TranslationsFunc.Tests.Services
{
    [TestClass]
    public class OpenAITranslationServiceTests
    {
        private readonly Fixture _fixture = FixtureFactory.CreateFixture();

        [TestMethod]
        public void CreatePrompt_WhenPartOfSpeechIsNotEmpty_AddsItToPrompt()
        {
            const string partOfSpeech = "PartOfSpeech";
            TranslationInput input = new TranslationInput("da", ["en", "ru"], "Word", "Meaning", partOfSpeech, []);

            var sut = _fixture.Create<OpenAITranslationService>();
            string result = sut.CreatePrompt(input);

            result.Should().Be("Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'. "
                + "The word, in this context, means: 'Meaning'. Provide between 1 and 3 possible answers so I can choose the best one. "
                + "Maintain the same part of speech in the translations. When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
        }

        [TestMethod]
        public void CreatePrompt_WhenPartOfSpeechIsEmpty_DoesNotAddItToPrompt()
        {
            const string partOfSpeech = "";
            TranslationInput input = new TranslationInput("da", ["en", "ru"], "Word", "Meaning", partOfSpeech, []);

            var sut = _fixture.Create<OpenAITranslationService>();
            string result = sut.CreatePrompt(input);

            result.Should().Be("Translate the word 'Word' from the language 'da' into the languages 'en', 'ru'. "
                + "The word, in this context, means: 'Meaning'. Provide between 1 and 3 possible answers so I can choose the best one. "
                + "When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
        }

        [TestMethod]
        public void CreatePrompt_WhenExamplesAreNotEmpty_AddsThemToPrompt()
        {
            IEnumerable<string> examples = ["example 1", "example 2"];
            TranslationInput input = new TranslationInput("da", ["en", "ru"], "Word", "Meaning", "PartOfSpeech", examples);

            var sut = _fixture.Create<OpenAITranslationService>();
            string result = sut.CreatePrompt(input);

            result.Should().Be("Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'. "
                + "The word, in this context, means: 'Meaning'. Provide between 1 and 3 possible answers so I can choose the best one. "
                + "Check also examples to get a better context: 'example 1', 'example 2'. "
                + "Maintain the same part of speech in the translations. When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
        }

        [TestMethod]
        public void CreatePrompt_WhenExamplesAreEmpty_DoesNotAddThemToPrompt()
        {
            IEnumerable<string> examples = [];
            TranslationInput input = new TranslationInput("da", ["en", "ru"], "Word", "Meaning", "PartOfSpeech", examples);

            var sut = _fixture.Create<OpenAITranslationService>();
            string result = sut.CreatePrompt(input);

            result.Should().Be("Translate the word 'Word' from the language 'da' into the languages 'en', 'ru', where the part of speech is: 'PartOfSpeech'. "
                + "The word, in this context, means: 'Meaning'. Provide between 1 and 3 possible answers so I can choose the best one. "
                + "Maintain the same part of speech in the translations. When translating to English and the part of the speech is a verb, include the infinitive marker 'to'.");
        }
    }
}
