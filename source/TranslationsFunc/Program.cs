using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TranslationsFunc.Models;
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
    })
    .Build();

host.Run();
