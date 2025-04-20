using System.Reflection;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenAI.Chat;
using TranslationsFunc.Models.Input;
using TranslationsFunc.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        services.Configure<LoggerFilterOptions>(options =>
        {
            // The Application Insights SDK adds a default logging filter that instructs ILogger to capture only Warning and more severe logs. Application Insights requires an explicit override.
            // Log levels can also be configured using appsettings.json. For more information, see https://learn.microsoft.com/en-us/azure/azure-monitor/app/worker-service#ilogger-logs
            LoggerFilterRule? toRemove = options.Rules.FirstOrDefault(rule =>
                rule.ProviderName == "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider");

            if (toRemove is not null)
            {
                options.Rules.Remove(toRemove);
            }
        });

        services.AddScoped<IOpenAITranslationService, OpenAITranslationService>();

        services.AddScoped<IValidator<TranslationInput>, TranslationInputValidator>();

        services.AddSingleton<ChatClient>(_ =>
        {
            string key = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OpenAI API key not found. Please make sure it is added to environment variables.");
            return new ChatClient(model: "gpt-4o-mini", apiKey: key);
        });
    })
    .Build();

// Log the version number
var logger = host.Services.GetRequiredService<ILogger<Program>>();
var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown version";
logger.LogInformation("Application started. Version: {Version}", version);
Console.WriteLine($"Application started. Version: {version}");

host.Run();
