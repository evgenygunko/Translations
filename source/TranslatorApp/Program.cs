using System.Reflection;
using CopyWords.Parsers;
using CopyWords.Parsers.Services;
using FluentValidation;
using OpenAI.Chat;
using OpenAI.Responses;
using TranslatorApp.Models;
using TranslatorApp.Services;

var builder = WebApplication.CreateBuilder(args);

Console.OutputEncoding = System.Text.Encoding.UTF8;

// Configure logging to include event IDs but exclude scopes
builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(options =>
{
    options.IncludeScopes = false;

#if DEBUG
    options.SingleLine = false;
#else
    options.SingleLine = true;
#endif

    options.TimestampFormat = "[yyyy-MM-dd HH:mm:ss] ";
    options.ColorBehavior = Microsoft.Extensions.Logging.Console.LoggerColorBehavior.Enabled;
});

// Configure OpenAI settings
builder.Services.Configure<OpenAIConfiguration>(
    builder.Configuration.GetSection(OpenAIConfiguration.SectionName));

// Add services to the container.
builder.Services.AddControllers();

// Add custom services
builder.Services.AddScoped<ITranslationsService, TranslationsService>();
builder.Services.AddScoped<IOpenAITranslationService, OpenAITranslationService>();
builder.Services.AddScoped<IOpenAITranslationService2, OpenAITranslationService2>();
builder.Services.AddScoped<IValidator<LookUpWordRequest>, LookUpWordRequestValidator>();
builder.Services.AddSingleton<ILookUpWord, LookUpWord>();
builder.Services.AddSingleton<IDDOPageParser, DDOPageParser>();
builder.Services.AddSingleton<ISpanishDictPageParser, SpanishDictPageParser>();

builder.Services.AddHttpClient<IFileDownloader, FileDownloader>();

var key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User);

if (string.IsNullOrEmpty(key))
{
    throw new InvalidOperationException("OpenAI API key not found. Please make sure it is added to environment variables.");
}

// "gpt-5-mini"
// "gpt-4.1-mini" - this is the fastest model as of now, faster than "gpt-4o-mini", but more expensive. And "gpt-5-mini" is crazy slow, sometimes takes 30 seconds to respond.
builder.Services.AddSingleton<ChatClient>(_ => new ChatClient(model: "gpt-4o-mini", apiKey: key));

#pragma warning disable OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

// For the OpenAI Response API, it doesn't matter which model you select here. It will use the model, selected in the prompts saved in the Dashboard: https://platform.openai.com/chat
builder.Services.AddSingleton<OpenAIResponseClient>(_ => new OpenAIResponseClient(model: "gpt-4o-mini", apiKey: key));

#pragma warning restore OPENAI001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Log the version number
var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown version";
Console.WriteLine($"Application started. Version: {version}");

var logger = app.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Application started. Version: {Version}", version);

app.MapGet("/", () => $"Translation app v. {version}");

app.Run();