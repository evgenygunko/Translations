using System.Reflection;
using CopyWords.Parsers;
using CopyWords.Parsers.Services;
using FluentValidation;
using OpenAI.Chat;
using TranslatorApp.Models.Input;
using TranslatorApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add custom services
builder.Services.AddScoped<IOpenAITranslationService, OpenAITranslationService>();
builder.Services.AddScoped<IValidator<TranslationInput>, TranslationInputValidator>();
builder.Services.AddScoped<IValidator<TranslatorApp.Models.Input.V1.TranslationInput>, TranslatorApp.Models.Input.V1.TranslationInputValidator>();
builder.Services.AddSingleton<ILookUpWord, LookUpWord>();
builder.Services.AddSingleton<IDDOPageParser, DDOPageParser>();
builder.Services.AddSingleton<ISpanishDictPageParser, SpanishDictPageParser>();

builder.Services.AddHttpClient<IFileDownloader, FileDownloader>();

builder.Services.AddSingleton<ChatClient>(_ =>
{
    var key = Environment.GetEnvironmentVariable("OPENAI_API_KEY")
        ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY", EnvironmentVariableTarget.User);

    if (string.IsNullOrEmpty(key))
    {
        throw new InvalidOperationException("OpenAI API key not found. Please make sure it is added to environment variables.");
    }

    return new ChatClient(model: "gpt-4o-mini", apiKey: key);
});

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
