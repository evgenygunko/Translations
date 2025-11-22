using System.Reflection;
using CopyWords.Parsers;
using CopyWords.Parsers.Services;
using FluentValidation;
using OpenAI.Chat;
using OpenAI.Responses;
using Serilog;
using TranslatorApp.Models;
using TranslatorApp.Services;

namespace TranslatorApp;

public static class Program
{
    public static async Task Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        var builder = WebApplication.CreateBuilder(args);

        GlobalSettings globalSettings = ReadGlobalSettings();

        // Configure Serilog
        var loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration);

        if (!string.IsNullOrEmpty(globalSettings.BetterStackToken) && !string.IsNullOrEmpty(globalSettings.BetterStackIngestingHost))
        {
            loggerConfiguration = loggerConfiguration.WriteTo.BetterStack(
                sourceToken: globalSettings.BetterStackToken,
                betterStackEndpoint: $"https://{globalSettings.BetterStackIngestingHost}"
            );
        }

        Log.Logger = loggerConfiguration.CreateLogger();

        try
        {
            builder.Services.AddSerilog();

            if (string.IsNullOrEmpty(globalSettings.RequestSecretCode))
            {
                throw new InvalidOperationException("Request secret code key not found. Please make sure it is added to appsettings.json or environment variables.");
            }

            // Configure OpenAI settings
            builder.Services.Configure<OpenAIConfiguration>(
                builder.Configuration.GetSection(OpenAIConfiguration.SectionName));

            // Add services to the container.
            builder.Services.AddControllers();

            // Add custom services
            builder.Services.AddScoped<ITranslationsService, TranslationsService>();
            builder.Services.AddScoped<IOpenAITranslationService, OpenAITranslationService>();
            builder.Services.AddScoped<IOpenAITranslationService2, OpenAITranslationService2>();
            builder.Services.AddScoped<ISoundService, SoundService>();
            builder.Services.AddScoped<IValidator<LookUpWordRequest>, LookUpWordRequestValidator>();
            builder.Services.AddSingleton<ILookUpWord, LookUpWord>();
            builder.Services.AddSingleton<IDDOPageParser, DDOPageParser>();
            builder.Services.AddSingleton<ISpanishDictPageParser, SpanishDictPageParser>();
            builder.Services.AddSingleton<IGlobalSettings>(globalSettings);
            builder.Services.AddSingleton<IFileIOService, FileIOService>();
            builder.Services.AddSingleton<IFFMpegWrapper, FFMpegWrapper>();

            builder.Services.AddHttpClient<IFileDownloader, FileDownloader>();

            if (string.IsNullOrEmpty(globalSettings.OpenAIApiKey))
            {
                throw new InvalidOperationException("OpenAI API key not found. Please make sure it is added to appsettings.json or environment variables.");
            }

            // "gpt-5-mini"
            // "gpt-4.1-mini" - this is the fastest model as of now, faster than "gpt-4o-mini", but more expensive. And "gpt-5-mini" is crazy slow, sometimes takes 30 seconds to respond.
            builder.Services.AddSingleton<ChatClient>(_ => new ChatClient(model: "gpt-4o-mini", apiKey: globalSettings.OpenAIApiKey));

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            // For the OpenAI Response API, it doesn't matter which model you select here. It will use the model, selected in the prompts saved in the Dashboard: https://platform.openai.com/chat
            builder.Services.AddSingleton<OpenAIResponseClient>(_ => new OpenAIResponseClient(model: "gpt-4o-mini", apiKey: globalSettings.OpenAIApiKey));

#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

            var app = builder.Build();

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            // Log the version number
            var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown version";
            Log.Information("Application started. Version: {Version}", version);

            app.MapGet("/", () => $"Translation app v. {version}");

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static GlobalSettings ReadGlobalSettings()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true)
            .Build();

        var globalSettings = new GlobalSettings();
        configuration.GetSection("GlobalSettings").Bind(globalSettings);

        if (string.IsNullOrEmpty(globalSettings.RequestSecretCode))
        {
            string? requestSecretCode = Environment.GetEnvironmentVariable("REQUEST_SECRET_CODE")
                ?? Environment.GetEnvironmentVariable("REQUEST_SECRET_CODE", EnvironmentVariableTarget.User);

            if (string.IsNullOrEmpty(requestSecretCode))
            {
                throw new InvalidOperationException("Request secret code key not found. Please make sure it is added to appsettings.json or environment variables.");
            }

            globalSettings.RequestSecretCode = requestSecretCode;
        }

        if (string.IsNullOrEmpty(globalSettings.OpenAIApiKey))
        {
            string? openAIApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
                ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User);

            if (string.IsNullOrEmpty(openAIApiKey))
            {
                throw new InvalidOperationException("OpenAI API key not found. Please make sure it is added to appsettings.json or environment variables.");
            }

            globalSettings.OpenAIApiKey = openAIApiKey;
        }

        if (!globalSettings.UseOpenAIResponseAPI.HasValue)
        {
            string? useResponseAPI = Environment.GetEnvironmentVariable("USE_OPENAI_RESPONSE_API")
                ?? Environment.GetEnvironmentVariable("USE_OPENAI_RESPONSE_API", EnvironmentVariableTarget.User);

            if (!string.IsNullOrEmpty(useResponseAPI))
            {
                globalSettings.UseOpenAIResponseAPI = useResponseAPI.Equals("true", StringComparison.OrdinalIgnoreCase)
                    || useResponseAPI.Equals("1", StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                globalSettings.UseOpenAIResponseAPI = false; // Default to false if not set
            }
        }

        // BetterStack settings are optional
        if (string.IsNullOrEmpty(globalSettings.BetterStackToken))
        {
            globalSettings.BetterStackToken = Environment.GetEnvironmentVariable("BETTERSTACK_TOKEN")
                ?? Environment.GetEnvironmentVariable("BETTERSTACK_TOKEN", EnvironmentVariableTarget.User);
        }

        if (string.IsNullOrEmpty(globalSettings.BetterStackIngestingHost))
        {
            globalSettings.BetterStackIngestingHost = Environment.GetEnvironmentVariable("BETTERSTACK_INGESTING_HOST")
                ?? Environment.GetEnvironmentVariable("BETTERSTACK_INGESTING_HOST", EnvironmentVariableTarget.User);
        }

        return globalSettings;
    }
}