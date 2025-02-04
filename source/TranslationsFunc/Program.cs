using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenAI.Chat;
using TranslationsFunc.Models.Input;
using TranslationsFunc.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.AddScoped<IAzureTranslationService, AzureTranslationService>();
        services.AddScoped<IOpenAITranslationService, OpenAITranslationService>();

        services.AddScoped<IValidator<TranslationInput>, TranslationInputValidator>();
        services.AddScoped<IValidator<TranslationInput2>, TranslationInput2Validator>();

        services.AddSingleton<ChatClient>(_ =>
        {
            string key = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OpenAI API key not found. Please make sure it is added to environment variables.");
            return new ChatClient(model: "gpt-4o-mini", apiKey: key);
        });
    })
    .Build();

host.Run();
