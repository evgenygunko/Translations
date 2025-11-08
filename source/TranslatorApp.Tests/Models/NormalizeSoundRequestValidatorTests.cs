// Ignore Spelling: Validator App

using AutoFixture;
using FluentAssertions;
using TranslatorApp.Models;

namespace TranslatorApp.Tests.Models
{
    [TestClass]
    public class NormalizeSoundRequestValidatorTests
    {
        private readonly IFixture _fixture = FixtureFactory.Create();

        [TestMethod]
        public void Validate_WhenAllRequiredFieldsHaveValues_ReturnsTrue()
        {
            var request = new NormalizeSoundRequest(SoundUrl: "https://example.com/sound.mp3", Word: "test", Version: "1");

            var sut = _fixture.Create<NormalizeSoundRequestValidator>();
            var result = sut.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void Validate_WhenWordIsNullOrEmpty_ReturnsFalse(string word)
        {
            var request = new NormalizeSoundRequest(SoundUrl: "https://example.com/sound.mp3", Word: word, Version: "1");

            var sut = _fixture.Create<NormalizeSoundRequestValidator>();
            var result = sut.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Word' must not be empty.");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        [DataRow("ftp://example.com/sound.mp3")]
        [DataRow("file:///C:/sounds/sound.mp3")]
        [DataRow("//example.com/sound.mp3")]
        public void Validate_WhenSoundUrlIsNotAValidUrl_ReturnsFalse(string soundUrl)
        {
            var request = new NormalizeSoundRequest(SoundUrl: soundUrl, Word: "test", Version: "1");

            var sut = _fixture.Create<NormalizeSoundRequestValidator>();
            var result = sut.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "'SoundUrl' must be a valid URL");
        }

        [TestMethod]
        public void Validate_WhenAllFieldsAreEmpty_ReturnsMultipleErrors()
        {
            var request = new NormalizeSoundRequest(SoundUrl: "", Word: "", Version: "1");

            var sut = _fixture.Create<NormalizeSoundRequestValidator>();
            var result = sut.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(2);
            result.Errors.Should().Contain(e => e.PropertyName == "SoundUrl");
            result.Errors.Should().Contain(e => e.PropertyName == "Word");
        }

        [TestMethod]
        [DataRow("test word")]
        [DataRow("test-word")]
        [DataRow("test_word")]
        [DataRow("testWord123")]
        [DataRow("TESTWORD")]
        [DataRow("tEstWoRd")]
        public void Validate_WhenWordContainsValidCharacters_ReturnsTrue(string word)
        {
            var request = new NormalizeSoundRequest(SoundUrl: "https://example.com/sound.mp3", Word: word, Version: "1");

            var sut = _fixture.Create<NormalizeSoundRequestValidator>();
            var result = sut.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenUrlHasQueryString_ReturnsTrue()
        {
            var request = new NormalizeSoundRequest(SoundUrl: "https://example.com/sound.mp3?version=1&quality=high", Word: "test", Version: "1");

            var sut = _fixture.Create<NormalizeSoundRequestValidator>();
            var result = sut.Validate(request);

            result.IsValid.Should().BeTrue();
        }
    }
}
