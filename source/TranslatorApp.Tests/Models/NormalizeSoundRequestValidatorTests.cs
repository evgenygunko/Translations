// Ignore Spelling: Validator App

using AutoFixture;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
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
            var request = new NormalizeSoundRequest(SoundUrl: "https://example.com/sound.mp3", Word: "test");

            var sut = _fixture.Create<NormalizeSoundRequestValidator>();
            ValidationResult result = sut.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void Validate_WhenSoundUrlIsNullOrEmpty_ReturnsFalse(string soundUrl)
        {
            var request = new NormalizeSoundRequest(SoundUrl: soundUrl, Word: "test");

            var sut = _fixture.Create<NormalizeSoundRequestValidator>();
            ValidationResult result = sut.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "'SoundUrl' must not be empty");
        }

        [TestMethod]
        [DataRow(null)]
        [DataRow("")]
        [DataRow(" ")]
        public void Validate_WhenWordIsNullOrEmpty_ReturnsFalse(string word)
        {
            var request = new NormalizeSoundRequest(SoundUrl: "https://example.com/sound.mp3", Word: word);

            var sut = _fixture.Create<NormalizeSoundRequestValidator>();
            ValidationResult result = sut.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "'Word' must not be empty.");
        }

        [TestMethod]
        [DataRow("https://example.com/sound.mp3")]
        [DataRow("http://test.org/audio.wav")]
        [DataRow("https://cdn.example.com/files/audio/sound.ogg")]
        public void Validate_WhenSoundUrlIsValidHttpUrl_ReturnsTrue(string soundUrl)
        {
            var request = new NormalizeSoundRequest(SoundUrl: soundUrl, Word: "test");

            var sut = _fixture.Create<NormalizeSoundRequestValidator>();
            ValidationResult result = sut.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        [DataRow("not-a-url")]
        [DataRow("ftp://example.com/sound.mp3")]
        [DataRow("file:///C:/sounds/audio.mp3")]
        [DataRow("example.com/sound.mp3")]
        [DataRow("www.example.com/sound.mp3")]
        public void Validate_WhenSoundUrlIsNotValidHttpUrl_ReturnsFalse(string soundUrl)
        {
            var request = new NormalizeSoundRequest(SoundUrl: soundUrl, Word: "test");

            var sut = _fixture.Create<NormalizeSoundRequestValidator>();
            ValidationResult result = sut.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.ErrorMessage == "'SoundUrl' must be a valid URL");
        }

        [TestMethod]
        public void Validate_WhenBothFieldsAreEmpty_ReturnsMultipleErrors()
        {
            var request = new NormalizeSoundRequest(SoundUrl: "", Word: "");

            var sut = _fixture.Create<NormalizeSoundRequestValidator>();
            ValidationResult result = sut.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCountGreaterOrEqualTo(2);
            result.Errors.Should().Contain(e => e.ErrorMessage == "'SoundUrl' must not be empty");
            result.Errors.Should().Contain(e => e.ErrorMessage == "'Word' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenRequestIsNull_ReturnsFalse()
        {
            var sut = _fixture.Create<NormalizeSoundRequestValidator>();
            var context = new ValidationContext<NormalizeSoundRequest>((NormalizeSoundRequest)null!);
            ValidationResult result = sut.Validate(context);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("Please ensure a model was supplied.");
        }

        [TestMethod]
        public void Validate_WhenSoundUrlIsValidButWordIsEmpty_ReturnsFalse()
        {
            var request = new NormalizeSoundRequest(SoundUrl: "https://example.com/sound.mp3", Word: "");

            var sut = _fixture.Create<NormalizeSoundRequestValidator>();
            ValidationResult result = sut.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'Word' must not be empty.");
        }

        [TestMethod]
        public void Validate_WhenWordIsValidButSoundUrlIsInvalid_ReturnsFalse()
        {
            var request = new NormalizeSoundRequest(SoundUrl: "not-a-valid-url", Word: "test");

            var sut = _fixture.Create<NormalizeSoundRequestValidator>();
            ValidationResult result = sut.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors.First().ErrorMessage.Should().Be("'SoundUrl' must be a valid URL");
        }

        [TestMethod]
        public void Validate_WhenWordContainsSpecialCharacters_ReturnsTrue()
        {
            var request = new NormalizeSoundRequest(SoundUrl: "https://example.com/sound.mp3", Word: "ordbog's");

            var sut = _fixture.Create<NormalizeSoundRequestValidator>();
            ValidationResult result = sut.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenSoundUrlHasQueryParameters_ReturnsTrue()
        {
            var request = new NormalizeSoundRequest(SoundUrl: "https://example.com/sound.mp3?version=1&quality=high", Word: "test");

            var sut = _fixture.Create<NormalizeSoundRequestValidator>();
            ValidationResult result = sut.Validate(request);

            result.IsValid.Should().BeTrue();
        }
    }
}
