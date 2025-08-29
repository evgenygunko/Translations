﻿// Ignore Spelling: Afeitar Coche App Slå

using AutoFixture;
using CopyWords.Parsers.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using TranslatorApp.Models;
using TranslatorApp.Services;

namespace TranslatorApp.Tests.Services
{
    [TestClass]
    public class TranslationsServiceTests
    {
        private readonly IFixture _fixture = FixtureFactory.Create();

        #region Tests for TranslateAsync

        [TestMethod]
        public void TranslateAsync_WhenUseOpenAIResponseAPIIsFalse_CallsOpenAITranslationService()
        {
            Environment.SetEnvironmentVariable("USE_OPENAI_RESPONSE_API", "false");
            string sourceLanguage = _fixture.Create<string>();

            WordModel wordModel = _fixture.Create<WordModel>();

            var openAITranslationServiceMock = _fixture.Freeze<Mock<IOpenAITranslationService>>();

            var sut = _fixture.Create<TranslationsService>();
            _ = sut.TranslateAsync(sourceLanguage, wordModel);

            openAITranslationServiceMock.Verify(x => x.TranslateAsync(It.IsAny<TranslatorApp.Models.Translation.TranslationInput>()));
        }

        [TestMethod]
        public void TranslateAsync_WhenUseOpenAIResponseAPIIsTrue_CallsOpenAITranslationService2()
        {
            Environment.SetEnvironmentVariable("USE_OPENAI_RESPONSE_API", "true");
            string sourceLanguage = _fixture.Create<string>();

            WordModel wordModel = _fixture.Create<WordModel>();

            var openAITranslationServiceMock = _fixture.Freeze<Mock<IOpenAITranslationService2>>();

            var sut = _fixture.Create<TranslationsService>();
            _ = sut.TranslateAsync(sourceLanguage, wordModel);

            openAITranslationServiceMock.Verify(x => x.TranslateAsync(It.IsAny<TranslatorApp.Models.Translation.TranslationInput>()));
        }

        #endregion

        #region Tests for CreateTranslationInputFromWordModel

        [TestMethod]
        public void CreateTranslationInputFromWordModel_Should_ReturnTranslationInput()
        {
            string sourceLanguage = SourceLanguage.Danish.ToString();
            string destinationLanguage = "Russian";
            WordModel wordModel = _fixture.Create<WordModel>();

            var sut = _fixture.Create<TranslationsService>();
            TranslatorApp.Models.Translation.TranslationInput result = sut.CreateTranslationInputFromWordModel(sourceLanguage, wordModel);

            result.Should().NotBeNull();
            result.Version.Should().Be("2");
            result.SourceLanguage.Should().Be(sourceLanguage.ToString());
            result.DestinationLanguage.Should().Be(destinationLanguage);

            int definitionIndex = 1;
            foreach (Definition definition in wordModel.Definitions)
            {
                // Headword is taken from the first meaning
                Meaning firstMeaning = definition.Contexts.First().Meanings.First();

                TranslatorApp.Models.Translation.DefinitionInput inputDefinition = result.Definitions.First(x => x.id == definitionIndex);

                inputDefinition.Headword.Text.Should().Be(definition.Headword.Original);
                inputDefinition.Headword.PartOfSpeech.Should().Be(definition.PartOfSpeech);
                inputDefinition.Headword.Meaning.Should().Be(firstMeaning.Original);
                inputDefinition.Headword.Examples.Should().HaveCount(firstMeaning.Examples.Count());

                inputDefinition.Contexts.Should().HaveCount(definition.Contexts.Count());

                int contextIndex = 1;
                foreach (Context context in definition.Contexts)
                {
                    TranslatorApp.Models.Translation.ContextInput inputContext = inputDefinition.Contexts.First(x => x.id == contextIndex);
                    inputContext.Meanings.Should().HaveCount(context.Meanings.Count());

                    TranslatorApp.Models.Translation.MeaningInput firstInputMeaning = inputContext.Meanings.First();
                    Meaning firstMeaningInContext = context.Meanings.First();

                    firstInputMeaning.Text.Should().Be(firstMeaningInContext.Original);
                    firstInputMeaning.Examples.Should().HaveCount(firstMeaningInContext.Examples.Count());
                    firstInputMeaning.id.Should().Be(1);

                    inputContext.Meanings.Last().id.Should().Be(inputContext.Meanings.Count());

                    contextIndex++;
                }

                definitionIndex++;
            }
        }

        [TestMethod]
        public void CreateTranslationInputFromWordModel_ForSlå_Adds2DefinitionsToTranslationInput()
        {
            string sourceLanguage = SourceLanguage.Danish.ToString();
            string destinationLanguage = "Russian";
            WordModel wordModel = CreateWordModelForSlå();

            var sut = _fixture.Create<TranslationsService>();
            TranslatorApp.Models.Translation.TranslationInput result = sut.CreateTranslationInputFromWordModel(sourceLanguage, wordModel);

            result.Should().NotBeNull();
            result.Version.Should().Be("2");
            result.SourceLanguage.Should().Be(sourceLanguage.ToString());
            result.DestinationLanguage.Should().Be(destinationLanguage);

            result.Definitions.Should().HaveCount(1);

            TranslatorApp.Models.Translation.DefinitionInput inputDefinition;
            TranslatorApp.Models.Translation.ContextInput inputContext;

            /***********************************************************************/
            // Afeitar
            /***********************************************************************/
            inputDefinition = result.Definitions.First();
            inputDefinition.id.Should().Be(1);
            inputDefinition.Headword.Text.Should().Be("slå om-nederdel");
            inputDefinition.Headword.PartOfSpeech.Should().Be("transitive verb");
            inputDefinition.Headword.Meaning.Should().Be("");
            inputDefinition.Headword.Examples.Should().HaveCount(0);

            inputContext = inputDefinition.Contexts.First();
            inputContext.Meanings.Should().HaveCount(0);
        }

        [TestMethod]
        public void CreateTranslationInputFromWordModel_ForAfeitar_Adds2DefinitionsToTranslationInput()
        {
            string sourceLanguage = SourceLanguage.Spanish.ToString();
            string destinationLanguage = "Russian";
            WordModel wordModel = CreateWordModelForAefitar();

            var sut = _fixture.Create<TranslationsService>();
            TranslatorApp.Models.Translation.TranslationInput result = sut.CreateTranslationInputFromWordModel(sourceLanguage, wordModel);

            result.Should().NotBeNull();
            result.Version.Should().Be("2");
            result.SourceLanguage.Should().Be(sourceLanguage.ToString());
            result.DestinationLanguage.Should().Be(destinationLanguage);

            result.Definitions.Should().HaveCount(2);

            TranslatorApp.Models.Translation.DefinitionInput inputDefinition;
            TranslatorApp.Models.Translation.ContextInput inputContext;
            TranslatorApp.Models.Translation.MeaningInput inputMeaning;

            /***********************************************************************/
            // Afeitar
            /***********************************************************************/
            inputDefinition = result.Definitions.First();
            inputDefinition.id.Should().Be(1);
            inputDefinition.Headword.Text.Should().Be("afeitar");
            inputDefinition.Headword.PartOfSpeech.Should().Be("transitive verb");
            inputDefinition.Headword.Meaning.Should().Be("to shave");
            inputDefinition.Headword.Examples.Should().HaveCount(1);

            inputContext = inputDefinition.Contexts.First();
            inputContext.Meanings.Should().HaveCount(1);
            inputMeaning = inputContext.Meanings.First();
            inputMeaning.id.Should().Be(1);
            inputMeaning.Text.Should().Be("to shave");
            inputMeaning.Examples.Should().HaveCount(1);
            inputMeaning.Examples.First().Should().Be("Para el verano, papá decidió afeitar al perro.");

            /***********************************************************************/
            // Afeitarse
            /***********************************************************************/
            inputDefinition = result.Definitions.Last();
            inputDefinition.id.Should().Be(2);
            inputDefinition.Headword.Text.Should().Be("afeitarse");
            inputDefinition.Headword.PartOfSpeech.Should().Be("reflexive verb");
            inputDefinition.Headword.Meaning.Should().Be("to shave");
            inputDefinition.Headword.Examples.Should().HaveCount(1);

            inputContext = inputDefinition.Contexts.First();
            inputContext.Meanings.Should().HaveCount(1);
            inputMeaning = inputContext.Meanings.First();
            inputMeaning.id.Should().Be(1);
            inputMeaning.Text.Should().Be("to shave");
            inputMeaning.Examples.Should().HaveCount(1);
            inputMeaning.Examples.First().Should().Be("¿Con qué frecuencia te afeitas la barba?");
        }

        [TestMethod]
        public void CreateTranslationInputFromWordModel_ForCoche_Adds4ContextsToTranslationInput()
        {
            string sourceLanguage = SourceLanguage.Spanish.ToString();
            string destinationLanguage = "Russian";
            WordModel wordModel = CreateWordModelForCoche();

            var sut = _fixture.Create<TranslationsService>();
            TranslatorApp.Models.Translation.TranslationInput result = sut.CreateTranslationInputFromWordModel(sourceLanguage, wordModel);

            result.Should().NotBeNull();
            result.Version.Should().Be("2");
            result.SourceLanguage.Should().Be(sourceLanguage.ToString());
            result.DestinationLanguage.Should().Be(destinationLanguage);

            result.Definitions.Should().HaveCount(1);

            TranslatorApp.Models.Translation.DefinitionInput inputDefinition = result.Definitions.First();
            inputDefinition.id.Should().Be(1);
            inputDefinition.Headword.Text.Should().Be("el coche");
            inputDefinition.Headword.PartOfSpeech.Should().Be("masculine noun");
            inputDefinition.Headword.Meaning.Should().Be("car");
            inputDefinition.Headword.Examples.Should().HaveCount(1);

            TranslatorApp.Models.Translation.ContextInput inputContext;
            TranslatorApp.Models.Translation.MeaningInput inputMeaning;

            /***********************************************************************/
            // Context 1
            /***********************************************************************/
            inputContext = inputDefinition.Contexts.First();
            inputContext.ContextString.Should().Be("(vehicle)");
            inputContext.Meanings.Should().HaveCount(1);
            inputMeaning = inputContext.Meanings.First();
            inputMeaning.id.Should().Be(1);
            inputMeaning.Text.Should().Be("car");
            inputMeaning.Examples.Should().HaveCount(1);
            inputMeaning.Examples.First().Should().Be("Mi coche no prende porque tiene una falla en el motor.");

            /***********************************************************************/
            // Context 2
            /***********************************************************************/
            inputContext = inputDefinition.Contexts.Skip(1).First();
            inputContext.ContextString.Should().Be("(vehicle led by horses)");
            inputContext.Meanings.Should().HaveCount(1);
            inputMeaning = inputContext.Meanings.First();
            inputMeaning.id.Should().Be(1);
            inputMeaning.Text.Should().Be("carriage");
            inputMeaning.Examples.Should().HaveCount(1);
            inputMeaning.Examples.First().Should().Be("Los monarcas llegaron en un coche elegante.");

            /***********************************************************************/
            // Context 3
            /***********************************************************************/
            inputContext = inputDefinition.Contexts.Skip(2).First();
            inputContext.ContextString.Should().Be("(train car)");
            inputContext.Meanings.Should().HaveCount(1);
            inputMeaning = inputContext.Meanings.First();
            inputMeaning.id.Should().Be(1);
            inputMeaning.Text.Should().Be("car");
            inputMeaning.Examples.Should().HaveCount(1);
            inputMeaning.Examples.First().Should().Be("Tu mamá y yo vamos a pasar al coche comedor para almorzar.");

            /***********************************************************************/
            // Context 4
            /***********************************************************************/
            inputContext = inputDefinition.Contexts.Skip(3).First();
            inputContext.ContextString.Should().Be("(for babies)");
            inputContext.Meanings.Should().HaveCount(1);
            inputMeaning = inputContext.Meanings.First();
            inputMeaning.id.Should().Be(1);
            inputMeaning.Text.Should().Be("stroller");
            inputMeaning.Examples.Should().HaveCount(1);
            inputMeaning.Examples.First().Should().Be("La niñita no se quería subir al coche. Quería ir caminando.");
        }

        #endregion

        #region Tests for CreateWordModelFromTranslationOutput

        [TestMethod]
        public void CreateWordModelFromTranslationOutput_WhenContextIsNotReturned_UsesContextFromOriginalModel()
        {
            var originalWordModel = CreateWordModelForSlå();

            var translationOutput = new TranslatorApp.Models.Translation.TranslationOutput(
                Definitions: [
                    new TranslatorApp.Models.Translation.DefinitionOutput(
                        id: 1,
                        HeadwordTranslation: _fixture.Create<string>(),
                        HeadwordTranslationEnglish: _fixture.Create<string>(),
                        Contexts: []
                    )
                ]
            );

            var loggerMock = _fixture.Freeze<Mock<ILogger<TranslationsService>>>();

            var sut = _fixture.Create<TranslationsService>();
            WordModel result = sut.CreateWordModelFromTranslationOutput(originalWordModel, translationOutput);

            result.Word.Should().Be(originalWordModel.Word);

            Context originalContext = originalWordModel.Definitions.First().Contexts.First();
            Context translatedContext = result.Definitions.First().Contexts.First();
            translatedContext.ContextEN.Should().Be(originalContext.ContextEN);
            translatedContext.Position.Should().Be(originalContext.Position);
            translatedContext.Meanings.Should().HaveCount(originalContext.Meanings.Count());

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.Is<EventId>(e => e.Id == (int)TranslatorAppEventId.OpenAPIDidNotReturnContext),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("OpenAPI did not return a context. Trying to find a context with id '1', but the returned object has '0' contexts with ids ''.")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()!),
                Times.Once);
        }

        [TestMethod]
        public void CreateWordModelFromTranslationOutput_Should_KeepWord()
        {
            var originalWordModel = new WordModel(
                Word: _fixture.Create<string>(),
                SoundUrl: _fixture.Create<Uri>().ToString(),
                SoundFileName: _fixture.Create<string>(),
                Definitions: [
                    new Definition(
                        Headword: new Headword(Original: _fixture.Create<string>(), English: null, Russian: null),
                        PartOfSpeech: _fixture.Create<string>(),
                        Endings: _fixture.Create<string>(),
                        Contexts: [new Context(ContextEN: _fixture.Create<string>(), Position: _fixture.Create<string>(), Meanings: [_fixture.Create<Meaning>()])]
                    ),
                ],
                Variations: _fixture.CreateMany<Variant>().ToArray()
            );

            var translationOutput = CreateOutputWithOneDefinition();

            var sut = _fixture.Create<TranslationsService>();
            WordModel result = sut.CreateWordModelFromTranslationOutput(originalWordModel, translationOutput);

            result.Word.Should().Be(originalWordModel.Word);
        }

        [TestMethod]
        public void CreateWordModelFromTranslationOutput_Should_KeepSoundUrl()
        {
            var originalWordModel = new WordModel(
                Word: _fixture.Create<string>(),
                SoundUrl: _fixture.Create<Uri>().ToString(),
                SoundFileName: _fixture.Create<string>(),
                Definitions: [
                    new Definition(
                        Headword: new Headword(Original: _fixture.Create<string>(), English: null, Russian: null),
                        PartOfSpeech: _fixture.Create<string>(),
                        Endings: _fixture.Create<string>(),
                        Contexts: [new Context(ContextEN: _fixture.Create<string>(), Position: _fixture.Create<string>(), Meanings: [_fixture.Create<Meaning>()])]
                    ),
                ],
                Variations: _fixture.CreateMany<Variant>().ToArray()
            );

            var translationOutput = CreateOutputWithOneDefinition();

            var sut = _fixture.Create<TranslationsService>();
            WordModel result = sut.CreateWordModelFromTranslationOutput(originalWordModel, translationOutput);

            result.SoundUrl.Should().Be(originalWordModel.SoundUrl);
        }

        [TestMethod]
        public void CreateWordModelFromTranslationOutput_Should_KeepSoundFileName()
        {
            var originalWordModel = new WordModel(
                Word: _fixture.Create<string>(),
                SoundUrl: _fixture.Create<Uri>().ToString(),
                SoundFileName: _fixture.Create<string>(),
                Definitions: [
                    new Definition(
                        Headword: new Headword(Original: _fixture.Create<string>(), English: null, Russian: null),
                        PartOfSpeech: _fixture.Create<string>(),
                        Endings: _fixture.Create<string>(),
                        Contexts: [new Context(ContextEN: _fixture.Create<string>(), Position: _fixture.Create<string>(), Meanings: [_fixture.Create<Meaning>()])]
                    ),
                ],
                Variations: _fixture.CreateMany<Variant>().ToArray()
            );

            var translationOutput = CreateOutputWithOneDefinition();

            var sut = _fixture.Create<TranslationsService>();
            WordModel result = sut.CreateWordModelFromTranslationOutput(originalWordModel, translationOutput);

            result.SoundFileName.Should().Be(originalWordModel.SoundFileName);
        }

        [TestMethod]
        public void CreateWordModelFromTranslationOutput_Should_KeepVariations()
        {
            var originalWordModel = new WordModel(
                Word: _fixture.Create<string>(),
                SoundUrl: _fixture.Create<Uri>().ToString(),
                SoundFileName: _fixture.Create<string>(),
                Definitions: [
                    new Definition(
                        Headword: new Headword(Original: _fixture.Create<string>(), English: null, Russian: null),
                        PartOfSpeech: _fixture.Create<string>(),
                        Endings: _fixture.Create<string>(),
                        Contexts: [new Context(ContextEN: _fixture.Create<string>(), Position: _fixture.Create<string>(), Meanings: [_fixture.Create<Meaning>()])]
                    ),
                ],
                Variations: _fixture.CreateMany<Variant>().ToArray()
            );

            var translationOutput = CreateOutputWithOneDefinition();

            var sut = _fixture.Create<TranslationsService>();
            WordModel result = sut.CreateWordModelFromTranslationOutput(originalWordModel, translationOutput);

            result.Variations.Should().BeEquivalentTo(originalWordModel.Variations);
        }

        [TestMethod]
        public void CreateWordModelFromTranslationOutput_Should_SetTranslations()
        {
            // Arrange
            var originalWordModel = new WordModel(
                Word: _fixture.Create<string>(),
                SoundUrl: _fixture.Create<Uri>().ToString(),
                SoundFileName: _fixture.Create<string>(),
                Definitions: [
                    new Definition(
                        Headword: new Headword(Original: _fixture.Create<string>(), English: null, Russian: null),
                        PartOfSpeech: _fixture.Create<string>(),
                        Endings: _fixture.Create<string>(),
                        Contexts: [new Context(ContextEN: _fixture.Create<string>(), Position: _fixture.Create<string>(), Meanings: [_fixture.Create<Meaning>()])]
                    ),
                ],
                Variations: _fixture.CreateMany<Variant>().ToArray()
            );

            var translationOutput = CreateOutputWithOneDefinition();

            // Act
            var sut = _fixture.Create<TranslationsService>();
            WordModel result = sut.CreateWordModelFromTranslationOutput(originalWordModel, translationOutput);

            // Assert
            result.Definitions.Should().HaveCount(1);

            Definition definition = result.Definitions.First();
            Definition originalDefinition = originalWordModel.Definitions.First();

            definition.PartOfSpeech.Should().Be(originalDefinition.PartOfSpeech);
            definition.Endings.Should().Be(originalDefinition.Endings);

            // Check headword
            definition.Headword.Original.Should().Be(originalDefinition.Headword.Original);
            definition.Headword.Russian.Should().NotBeEmpty(); // <-- should be translated
            definition.Headword.English.Should().NotBeEmpty(); // <-- should be translated

            // Check contexts
            definition.Contexts.Should().HaveCount(originalDefinition.Contexts.Count());
            IEnumerator<Context> originalContextsEnumerator = originalDefinition.Contexts.GetEnumerator();
            foreach (Context context in definition.Contexts)
            {
                originalContextsEnumerator.MoveNext();
                Context originalContext = originalContextsEnumerator.Current;

                context.ContextEN.Should().Be(originalContext.ContextEN);
                context.Position.Should().Be(originalContext.Position);

                // Check meanings
                context.Meanings.Should().HaveCount(originalContext.Meanings.Count());
                IEnumerator<Meaning> originalMeaningsEnumerator = originalContext.Meanings.GetEnumerator();

                foreach (Meaning meaning in context.Meanings)
                {
                    originalMeaningsEnumerator.MoveNext();
                    Meaning originalMeaning = originalMeaningsEnumerator.Current;

                    meaning.Original.Should().Be(originalMeaning.Original);

                    meaning.Translation.Should().NotBeEmpty(); // <-- should be translated
                    meaning.AlphabeticalPosition.Should().Be(originalMeaning.AlphabeticalPosition);
                    meaning.Tag.Should().Be(originalMeaning.Tag);
                    meaning.ImageUrl.Should().Be(originalMeaning.ImageUrl);

                    // Check examples
                    meaning.Examples.Should().BeEquivalentTo(originalMeaning.Examples);
                    IEnumerator<Example> originalExamplesEnumerator = originalMeaning.Examples.GetEnumerator();
                    foreach (Example example in meaning.Examples)
                    {
                        originalExamplesEnumerator.MoveNext();
                        Example originalExample = originalExamplesEnumerator.Current;

                        example.Original.Should().Be(originalExample.Original);
                        example.Translation.Should().Be(originalExample.Translation);
                    }
                }
            }
        }

        [TestMethod]
        public void CreateWordModelFromTranslationOutput_ForAfeitar_Returns2Definitions()
        {
            // Arrange
            WordModel originalWordModel = CreateWordModelForAefitar();

            var translationOutput = new TranslatorApp.Models.Translation.TranslationOutput(
                [
                    new TranslatorApp.Models.Translation.DefinitionOutput(
                        id: 1,
                        HeadwordTranslation: "брить",
                        HeadwordTranslationEnglish: "to shave",
                        Contexts: [
                            new TranslatorApp.Models.Translation.ContextOutput(
                                id: 1,
                                Meanings: [
                                    new TranslatorApp.Models.Translation.MeaningOutput(
                                        id: 1,
                                        MeaningTranslation: "брить"
                                    )
                                ]
                            )
                        ]
                    ),
                    new TranslatorApp.Models.Translation.DefinitionOutput(
                        id: 2,
                        HeadwordTranslation: "брить себя",
                        HeadwordTranslationEnglish: "to shave oneself, to shave",
                        Contexts: [
                            new TranslatorApp.Models.Translation.ContextOutput(
                                id: 1,
                                Meanings: [
                                    new TranslatorApp.Models.Translation.MeaningOutput(
                                        id: 1,
                                        MeaningTranslation: "бриться (бриться самому)"
                                    )
                                ]
                            )
                        ]
                    )
                ]
            );

            // Act
            var sut = _fixture.Create<TranslationsService>();
            WordModel result = sut.CreateWordModelFromTranslationOutput(originalWordModel, translationOutput);

            // Assert
            result.Definitions.Should().HaveCount(2);

            Definition definition;

            /***********************************************************************/
            // Afeitar
            /***********************************************************************/
            definition = result.Definitions.First();
            definition.PartOfSpeech.Should().Be("transitive verb");
            definition.Endings.Should().BeEmpty();

            // Check headword
            definition.Headword.Original.Should().Be("afeitar");
            definition.Headword.Russian.Should().Be("брить");
            definition.Headword.English.Should().Be("to shave");

            // Check contexts
            definition.Contexts.Should().HaveCount(1);
            Context context = definition.Contexts.First();
            context.ContextEN.Should().Be("(to remove hair)");
            context.Position.Should().Be("1");

            // Check meanings
            context.Meanings.Should().HaveCount(1);
            Meaning meaning = context.Meanings.First();
            meaning.Original.Should().Be("to shave");
            meaning.Translation.Should().Be("брить");
            meaning.AlphabeticalPosition.Should().Be("a.");
            meaning.Tag.Should().BeNull();
            meaning.ImageUrl.Should().BeNull();
            meaning.Examples.Should().HaveCount(1);
            meaning.Examples.First().Original.Should().Be("Para el verano, papá decidió afeitar al perro.");
            meaning.Examples.First().Translation.Should().BeNull();

            /***********************************************************************/
            // Afeitarse
            /***********************************************************************/
            definition = result.Definitions.Last();
            definition.PartOfSpeech.Should().Be("reflexive verb");
            definition.Endings.Should().BeEmpty();

            // Check headword
            definition.Headword.Original.Should().Be("afeitarse");
            definition.Headword.Russian.Should().Be("брить себя");
            definition.Headword.English.Should().Be("to shave oneself, to shave");

            // Check contexts
            definition.Contexts.Should().HaveCount(1);
            context = definition.Contexts.First();
            context.ContextEN.Should().Be("(to shave oneself)");
            context.Position.Should().Be("1");

            // Check meanings
            context.Meanings.Should().HaveCount(1);
            meaning = context.Meanings.First();
            meaning.Original.Should().Be("to shave");
            meaning.Translation.Should().Be("бриться (бриться самому)");
            meaning.AlphabeticalPosition.Should().Be("a.");
            meaning.Tag.Should().BeNull();
            meaning.ImageUrl.Should().BeNull();
            meaning.Examples.Should().HaveCount(1);
            meaning.Examples.First().Original.Should().Be("¿Con qué frecuencia te afeitas la barba?");
            meaning.Examples.First().Translation.Should().BeNull();
        }

        [TestMethod]
        public void CreateWordModelFromTranslationOutput_ForCoche_Returns4Contexts()
        {
            // Arrange
            WordModel originalWordModel = CreateWordModelForCoche();

            var translationOutput = new TranslatorApp.Models.Translation.TranslationOutput(
                [
                    new TranslatorApp.Models.Translation.DefinitionOutput(
                        id: 1,
                        HeadwordTranslation: "автомобиль",
                        HeadwordTranslationEnglish: "car",
                        Contexts: [
                            new TranslatorApp.Models.Translation.ContextOutput(
                                id: 1,
                                Meanings: [
                                    new TranslatorApp.Models.Translation.MeaningOutput(
                                        id: 1,
                                        MeaningTranslation: "автомобиль"
                                    )
                                ]
                            ),
                            new TranslatorApp.Models.Translation.ContextOutput(
                                id: 2,
                                Meanings: [
                                    new TranslatorApp.Models.Translation.MeaningOutput(
                                        id: 1,
                                        MeaningTranslation: "повозка"
                                    )
                                ]
                            ),
                            new TranslatorApp.Models.Translation.ContextOutput(
                                id: 3,
                                Meanings: [
                                    new TranslatorApp.Models.Translation.MeaningOutput(
                                        id: 1,
                                        MeaningTranslation: "вагон"
                                    )
                                ]
                            ),
                            new TranslatorApp.Models.Translation.ContextOutput(
                                id: 4,
                                Meanings: [
                                    new TranslatorApp.Models.Translation.MeaningOutput(
                                        id: 1,
                                        MeaningTranslation: "коляска"
                                    )
                                ]
                            )
                        ]
                    )
                ]
            );

            // Act
            var sut = _fixture.Create<TranslationsService>();
            WordModel result = sut.CreateWordModelFromTranslationOutput(originalWordModel, translationOutput);

            // Assert
            result.Definitions.Should().HaveCount(1);

            Definition definition = result.Definitions.First();

            /***********************************************************************/
            // Afeitar
            /***********************************************************************/
            definition.PartOfSpeech.Should().Be("masculine noun");
            definition.Endings.Should().BeEmpty();

            // Check headword
            definition.Headword.Original.Should().Be("el coche");
            definition.Headword.Russian.Should().Be("автомобиль");
            definition.Headword.English.Should().Be("car");

            // Check contexts
            definition.Contexts.Should().HaveCount(4);

            /***********************************************************************/
            // Context 1
            /***********************************************************************/
            Context context = definition.Contexts.First();
            context.Position.Should().Be("1");
            context.ContextEN.Should().Be("(vehicle)");
            context.Meanings.Should().HaveCount(1);
            Meaning meaning = context.Meanings.First();
            meaning.Original.Should().Be("car");
            meaning.Translation.Should().Be("автомобиль");
            meaning.AlphabeticalPosition.Should().Be("a.");
            meaning.Examples.Should().HaveCount(1);
            meaning.Examples.First().Original.Should().Be("Mi coche no prende porque tiene una falla en el motor.");
            meaning.Examples.First().Translation.Should().BeNull();

            /***********************************************************************/
            // Context 2
            /***********************************************************************/
            context = definition.Contexts.Skip(1).First();
            context.Position.Should().Be("2");
            context.ContextEN.Should().Be("(vehicle led by horses)");
            context.Meanings.Should().HaveCount(1);
            meaning = context.Meanings.First();
            meaning.Original.Should().Be("carriage");
            meaning.Translation.Should().Be("повозка");
            meaning.AlphabeticalPosition.Should().Be("a.");
            meaning.Examples.Should().HaveCount(1);
            meaning.Examples.First().Original.Should().Be("Los monarcas llegaron en un coche elegante.");
            meaning.Examples.First().Translation.Should().BeNull();

            /***********************************************************************/
            // Context 3
            /***********************************************************************/
            context = definition.Contexts.Skip(2).First();
            context.Position.Should().Be("3");
            context.ContextEN.Should().Be("(train car)");
            context.Meanings.Should().HaveCount(1);
            meaning = context.Meanings.First();
            meaning.Original.Should().Be("car");
            meaning.Translation.Should().Be("вагон");
            meaning.AlphabeticalPosition.Should().Be("a.");
            meaning.Examples.Should().HaveCount(1);
            meaning.Examples.First().Original.Should().Be("Tu mamá y yo vamos a pasar al coche comedor para almorzar.");
            meaning.Examples.First().Translation.Should().BeNull();

            /***********************************************************************/
            // Context 4
            /***********************************************************************/
            context = definition.Contexts.Skip(3).First();
            context.Position.Should().Be("4");
            context.ContextEN.Should().Be("(for babies)");
            context.Meanings.Should().HaveCount(1);
            meaning = context.Meanings.First();
            meaning.Original.Should().Be("stroller");
            meaning.Translation.Should().Be("коляска");
            meaning.AlphabeticalPosition.Should().Be("a.");
            meaning.Examples.Should().HaveCount(1);
            meaning.Examples.First().Original.Should().Be("La niñita no se quería subir al coche. Quería ir caminando.");
            meaning.Examples.First().Translation.Should().BeNull();
        }

        #endregion

        #region Private Methods

        private WordModel CreateWordModelForSlå()
        {
            WordModel wordModel = new WordModel(
                Word: "afeitar",
                SoundUrl: _fixture.Create<Uri>().ToString(),
                SoundFileName: _fixture.Create<string>(),
                Definitions: new[]
                {
                    new Definition(
                        Headword: new Headword(Original: "slå om-nederdel", English: null, Russian: null),
                        PartOfSpeech: "transitive verb",
                        Endings: "",
                        Contexts: new[]
                        {
                            new Context(
                                ContextEN: "",
                                Position: "1",
                                Meanings: Enumerable.Empty<Meaning>())
                        })
                },
                Variations: Enumerable.Empty<Variant>());

            return wordModel;
        }

        private WordModel CreateWordModelForAefitar()
        {
            WordModel wordModel = new WordModel(
                Word: "afeitar",
                SoundUrl: _fixture.Create<Uri>().ToString(),
                SoundFileName: _fixture.Create<string>(),
                Definitions: new[]
                {
                    new Definition(
                        Headword: new Headword(Original: "afeitar", English: null, Russian: null),
                        PartOfSpeech: "transitive verb",
                        Endings: "",
                        Contexts: new[]
                        {
                            new Context(
                                ContextEN: "(to remove hair)",
                                Position: "1",
                                Meanings: new[]
                                {
                                    new Meaning(
                                        Original: "to shave",
                                        Translation: null,
                                        AlphabeticalPosition: "a.",
                                        Tag: null,
                                        ImageUrl: null,
                                        Examples: [ new Example(Original: "Para el verano, papá decidió afeitar al perro.", Translation: null) ]
                                    )
                                })
                        }),
                    new Definition(
                        Headword: new Headword(Original: "afeitarse", English: null, Russian: null),
                        PartOfSpeech: "reflexive verb",
                        Endings: "",
                        Contexts: new[]
                        {
                            new Context(
                                ContextEN: "(to shave oneself)",
                                Position: "1",
                                Meanings: new[]
                                {
                                    new Meaning(
                                        Original: "to shave",
                                        Translation: null,
                                        AlphabeticalPosition: "a.",
                                        Tag: null,
                                        ImageUrl: null,
                                        Examples: [ new Example(Original: "¿Con qué frecuencia te afeitas la barba?", Translation: null) ]
                                    )
                                })
                        })
                },
                Variations: Enumerable.Empty<Variant>());

            return wordModel;
        }

        private WordModel CreateWordModelForCoche()
        {
            WordModel wordModel = new WordModel(
                Word: "el coche",
                SoundUrl: _fixture.Create<Uri>().ToString(),
                SoundFileName: _fixture.Create<string>(),
                Definitions: new[]
                {
                    new Definition(
                        Headword: new Headword(Original: "el coche", English: null, Russian: null),
                        PartOfSpeech: "masculine noun",
                        Endings: "",
                        Contexts: [
                            new Context(
                                ContextEN: "(vehicle)",
                                Position: "1",
                                Meanings: [
                                    new Meaning(
                                        Original: "car",
                                        Translation: null,
                                        AlphabeticalPosition: "a.",
                                        Tag: null,
                                        ImageUrl: null,
                                        Examples: [ new Example(Original: "Mi coche no prende porque tiene una falla en el motor.", Translation: null) ]
                                    )
                                ]),
                            new Context(
                                ContextEN: "(vehicle led by horses)",
                                Position: "2",
                                Meanings: [
                                    new Meaning(
                                        Original: "carriage",
                                        Translation: null,
                                        AlphabeticalPosition: "a.",
                                        Tag: null,
                                        ImageUrl: null,
                                        Examples: [ new Example(Original: "Los monarcas llegaron en un coche elegante.", Translation: null) ]
                                    )
                                ]),
                            new Context(
                                ContextEN: "(train car)",
                                Position: "3",
                                Meanings: [
                                    new Meaning(
                                        Original: "car",
                                        Translation: null,
                                        AlphabeticalPosition: "a.",
                                        Tag: null,
                                        ImageUrl: null,
                                        Examples: [ new Example(Original: "Tu mamá y yo vamos a pasar al coche comedor para almorzar.", Translation: null) ]
                                    )
                                ]),
                            new Context(
                                ContextEN: "(for babies)",
                                Position: "4",
                                Meanings: [
                                    new Meaning(
                                        Original: "stroller",
                                        Translation: null,
                                        AlphabeticalPosition: "a.",
                                        Tag: null,
                                        ImageUrl: null,
                                        Examples: [ new Example(Original: "La niñita no se quería subir al coche. Quería ir caminando.", Translation: null) ]
                                    )
                                ])
                        ]
                    )
                },
                Variations: Enumerable.Empty<Variant>());

            return wordModel;
        }

        private TranslatorApp.Models.Translation.TranslationOutput CreateOutputWithOneDefinition()
        {
            var translationOutput = new TranslatorApp.Models.Translation.TranslationOutput(
                Definitions: [
                    new TranslatorApp.Models.Translation.DefinitionOutput(
                        id: 1,
                        HeadwordTranslation: _fixture.Create<string>(),
                        HeadwordTranslationEnglish: _fixture.Create<string>(),
                        Contexts: [
                            new TranslatorApp.Models.Translation.ContextOutput(
                                id: 1,
                                Meanings: [
                                    new TranslatorApp.Models.Translation.MeaningOutput(
                                        id: 0,
                                        MeaningTranslation: _fixture.Create<string>())
                                ]
                            )
                        ]
                    )
                ]
            );

            return translationOutput;
        }

        #endregion
    }
}
