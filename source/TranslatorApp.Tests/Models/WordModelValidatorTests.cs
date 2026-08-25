// Ignore Spelling: Validator

using CopyWords.Parsers.Models;
using FluentAssertions;
using FluentValidation.Results;
using TranslatorApp.Models;

namespace TranslatorApp.Tests.Models
{
    [TestClass]
    public class WordModelValidatorTests
    {
        [TestMethod]
        public void Validate_WhenModelIsStructurallyUsable_ReturnsTrue()
        {
            WordModel wordModel = CreateUsableWordModel();
            var sut = new WordModelValidator();

            ValidationResult result = sut.Validate(wordModel);

            result.IsValid.Should().BeTrue();
        }

        [TestMethod]
        public void Validate_WhenModelIsNull_ReturnsFalse()
        {
            var sut = new WordModelValidator();

            ValidationResult result = sut.Validate((WordModel)null!);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle()
                .Which.ErrorMessage.Should().Be("Please ensure a model was supplied.");
        }

        [TestMethod]
        public void Validate_WhenRequiredStructureIsInvalid_ReturnsFalse()
        {
            WordModel valid = CreateUsableWordModel();
            Context validContext = valid.Definition.Contexts.Single();
            Meaning validMeaning = validContext.Meanings.Single();
            var unusableModels = new Dictionary<string, WordModel>
            {
                ["empty word"] = valid with { Word = " " },
                ["undefined source language"] = valid with { SourceLanguage = (SourceLanguage)999 },
                ["missing definition"] = valid with { Definition = null! },
                ["missing headword"] = valid with { Definition = valid.Definition with { Headword = null! } },
                ["empty original headword"] = valid with { Definition = valid.Definition with { Headword = valid.Definition.Headword with { Original = " " } } },
                ["missing contexts"] = valid with { Definition = valid.Definition with { Contexts = null! } },
                ["empty contexts"] = valid with { Definition = valid.Definition with { Contexts = [] } },
                ["null context"] = valid with { Definition = valid.Definition with { Contexts = [null!] } },
                ["missing meanings"] = valid with { Definition = valid.Definition with { Contexts = [validContext with { Meanings = null! }] } },
                ["null meaning"] = valid with { Definition = valid.Definition with { Contexts = [validContext with { Meanings = [null!] }] } },
                ["empty original meaning"] = valid with { Definition = valid.Definition with { Contexts = [validContext with { Meanings = [validMeaning with { Original = " " }] }] } },
                ["missing examples"] = valid with { Definition = valid.Definition with { Contexts = [validContext with { Meanings = [validMeaning with { Examples = null! }] }] } },
                ["missing variants"] = valid with { Variants = null! },
                ["missing expressions"] = valid with { Expressions = null! }
            };
            var sut = new WordModelValidator();

            foreach ((string scenario, WordModel wordModel) in unusableModels)
            {
                ValidationResult result = sut.Validate(wordModel);

                result.IsValid.Should().BeFalse(scenario);
            }
        }

        private static WordModel CreateUsableWordModel()
        {
            var definition = new Definition(
                new Headword("haj", null, null),
                "substantiv, fælleskøn",
                "-en, -er, -erne",
                [new Context("", "", [new Meaning("stor bruskfisk", null, "1", null, null, null, [])])]);

            return new WordModel("haj", SourceLanguage.Danish, null, null, definition, [], []);
        }
    }
}
