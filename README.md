# Translation App (for use by CopyWords App)

This project contains an ASP.NET Core Web API application that provides translation and word lookup services using the [OpenAI API](https://platform.openai.com/docs/overview). The application serves as a translation service for the CopyWords application.

The API provides HTTP endpoints for translating text and looking up word definitions, utilizing OpenAI's language models for accurate translations and definitions. The service can be easily extended to support additional translation providers if needed.

## Prerequisites

### OpenAI API Key

You will need an OpenAI API key to use this service:

1. Go to [OpenAI Platform](https://platform.openai.com)
2. Sign up or log in to your account
3. Navigate to API Keys section
4. Create a new API key and save it securely

### .NET Requirements

- .NET 9.0 SDK or later
- Visual Studio 2022 or Visual Studio Code

## Working with OpenAI API

This application provides two different approaches for integrating with OpenAI's services, each with its own advantages and use cases. You can switch between them using an environment variable.

### Option 1: Chat Completions API (Traditional) - Default

The `OpenAITranslationService` class uses OpenAI's standard Chat Completions API with structured output via JSON schema. This is the default option used when no additional configuration is set.

### Option 2: Response API (Experimental)

The `OpenAITranslationService2` class uses OpenAI's newer, experimental Response API. This approach:

- Uses the `/v1/responses` endpoint (currently in beta)
- Requires prompts to be pre-created and managed in the OpenAI dashboard
- References prompts by their unique ID rather than sending prompt text
- Provides centralized prompt management through OpenAI's interface
- Is ideal for applications with stable, reusable prompts

**Setting up Response API:**

1. Navigate to the [OpenAI Platform Dashboard](https://platform.openai.com/chat)
2. Create a new prompt with the required variables (`source_language`, `destination_langauge`, `input_json`)
3. Copy the prompt ID from the dashboard
4. Update the prompt ID in `OpenAITranslationService2.GetPromptMessage()` method

**Note:** The Response API is currently experimental and subject to change.

### Switching Between Options

The application automatically selects which service to use based on the `USE_OPENAI_RESPONSE_API` environment variable:

- **Option 1 (Default)**: No environment variable set or set to `false`/`0`
- **Option 2**: Set `USE_OPENAI_RESPONSE_API` to `true` or `1`

#### To use Option 1 (Chat Completions API):

```powershell
# Remove the environment variable or set it to false
[Environment]::SetEnvironmentVariable("USE_OPENAI_RESPONSE_API", $null, "User")
# OR
[Environment]::SetEnvironmentVariable("USE_OPENAI_RESPONSE_API", "false", "User")
```

#### To use Option 2 (Response API):

```powershell
[Environment]::SetEnvironmentVariable("USE_OPENAI_RESPONSE_API", "true", "User")
```

The application checks for this environment variable at both the process level and user level, so you can set it using either `"Process"` or `"User"` scope.

## Configure Local Development Environment

For local development and testing, you need to set up the OpenAI API key and configure the HTTP test environment.

### 1. Set OpenAI API Key

The application requires an OpenAI API key to be set as an environment variable. You can set it in one of the following ways:

#### Option A: User Environment Variable (Recommended for development)

```powershell
[Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "your-api-key-here", "User")
```

#### Option B: System Environment Variable

```powershell
[Environment]::SetEnvironmentVariable("OPENAI_API_KEY", "your-api-key-here", "Machine")
```

#### Option C: Using Visual Studio User Secrets

Right-click on the `TranslatorApp` project in Visual Studio and select "Manage User Secrets", then add:

```json
{
  "OPENAI_API_KEY": "your-api-key-here"
}
```

### 2. Configure HTTP Testing Environment

The project includes HTTP test files for testing the API. The `http-client.env.json` file contains environment configurations:

```json
{
  "dev": {
    "HostAddress": "http://localhost:5132"
  },
  "dev-docker": {
    "HostAddress": "http://localhost:8080"
  },
  "remote-host": {
    "HostAddress": "https://..."
  }
}
```

### 3. Running the Application

1. Open the solution in Visual Studio 2022
2. Ensure the OpenAI API key is configured (see step 1)
3. Press F5 or click "Start Debugging" to run the application
4. The API will be available at `http://localhost:5132` (or the port shown in the console)

### 4. Testing the API

Open the HTTP test files in the `HttpTests` folder:

- `TranslationController.LookUpWord.http` - Test word lookup functionality
- `Version.http` - Test version endpoint

Select the **dev** environment and send test requests. If everything is configured correctly, you will receive responses with translations and word definitions.

![Test in Visual Studio](https://raw.githubusercontent.com/evgenygunko/Translations/master/img/Test_From_VS.png)

## Docker Support

The application includes Docker support for containerized deployment.

### Building the Docker Image

```bash
docker build -t translator-app .
```

### Running with Docker

```bash
docker run -d -p 8080:8080 --env OPENAI_API_KEY=your-api-key-here translator-app
```

The application will be available at `http://localhost:8080`.

## Production Deployment

The project is currently configured for deployment to DigitalOcean App Platform.

## API Endpoints

The application provides the following endpoints:

### POST /api/LookUpWord

Looks up word definitions and translations.

**Request Body:**

```json
{
  "Text": "såsom",
  "SourceLanguage": "Danish",
  "DestinationLanguage": "Russian",
  "Version": "1"
}
```

**Response:**

```json
{
  "word": "såsom",
  "soundUrl": "https://static.ordnet.dk/mp3/11052/11052560_1.mp3",
  "soundFileName": "såsom.mp3",
  "definitions": [
    {
      "headword": {
        "original": "såsom",
        "english": "as, for example, such as",
        "russian": "как, например, в качестве"
      },
      "partOfSpeech": "konjunktion",
      "endings": "",
      "contexts": [
        {
          "contextEN": "",
          "position": "",
          "meanings": [
            {
              "original": "bruges til angivelse af et eller flere eksempler på noget",
              "translation": "используется для указания одного или нескольких примеров чего-либо",
              "alphabeticalPosition": "1",
              "tag": null,
              "imageUrl": null,
              "examples": [
                {
                  "original": "Festdragterne blev anvendt til større fester, såsom konfirmationer, bryllupper og dans omkring majstangen.",
                  "translation": null
                }
              ]
            },
            {
              "original": "bruges som indledning til en ledsætning der angiver en begrundelse",
              "translation": "используется как вводное слово в придаточном предложении, выражающем причину",
              "alphabeticalPosition": "2",
              "tag": null,
              "imageUrl": null,
              "examples": [
                {
                  "original": "han .. var sit firmas dygtigste sælger, såsom han flere år i træk havde præsteret de flotteste salgstal.",
                  "translation": null
                }
              ]
            }
          ]
        }
      ]
    }
  ],
  "variations": [
    {
      "word": "såsom konj.",
      "url": "https://ordnet.dk/ddo/ordbog?select=s%C3%A5som&query=s%C3%A5som"
    }
  ]
}
```

## Using CI/CD

The repository includes build configurations that can be used with various CI/CD platforms. You will need to configure:

- Source control integration
- Build pipeline configuration
- Deployment target settings
- Environment variables (especially `OPENAI_API_KEY`)

For Azure DevOps, GitHub Actions, or other CI/CD platforms, make sure to:

1. Configure the build pipeline to restore, build, and test the solution
2. Set up deployment to your chosen hosting platform
3. Configure the `OPENAI_API_KEY` secret in your CI/CD environment
