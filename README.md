# Translation App (for use by CopyWords App)

This project contains the ASP.NET Core Web API used by the CopyWords client for OpenAI-backed translation, suggested words, and sound-file downloads.

Online dictionary lookup and parsing are owned by the client application. The client sends an already-parsed `WordModel` to this API, which adds translations without fetching or parsing dictionary pages.

## Prerequisites

### OpenAI API Key

You will need an OpenAI API key to use this service:

1. Go to [OpenAI Platform](https://platform.openai.com)
2. Sign up or log in to your account
3. Navigate to API Keys section
4. Create a new API key and save it securely

### .NET Requirements

- .NET 10.0 SDK or later
- Visual Studio 2022 or Visual Studio Code

## Working with OpenAI API

The `OpenAITranslationService` class uses the Chat Completions API. Translation prompts live in source control and each request includes a strict JSON schema generated from its expected C# response type.

## Configure Local Development Environment

### Model rollout

Create a boolean LaunchDarkly flag named `use-gpt-6-luna` in the environment used by
`LAUNCHDARKLY_SDK_KEY`, initially false with an off variation of false. The existing
evaluation context has kind `user` and key `TranslationApp`.

- False: `gpt-4.1-mini` with existing request options.
- True: `gpt-6-luna` with `reasoning_effort: "none"`.

The flag is evaluated on every translation and suggested-words call. Missing flags,
uninitialized LaunchDarkly, and flag evaluation errors default to the existing model.
Both paths preserve Chat Completions, prompts, and strict JSON schemas. Toggle false
to roll back without restarting. Verify model access, translation quality, and latency
with the Danish/Spanish lookup and suggested-words samples under
`source/TranslatorApp/HttpTests` before enabling in production. Complete rollout before
January 14, 2027, when the legacy model retires and the false path stops working.

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

- `TranslationControllerV3_SuggestedWords.http` - Test suggested-word generation
- `SoundControllerV1.http` - Test sound downloads and MP4 conversion
- `Version.http` - Test version reporting

The v3 lookup endpoint accepts a `WordModel` that was populated by the client-side dictionary parser; it does not perform dictionary lookup on the server.

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

All functional endpoints require the configured request code in the `code` query parameter.

### POST /api/v3/Translation/LookUpWord

Accepts the client's parsed `WordModel`, translates its headword and meanings, and returns the same model shape with translation fields populated. The request and response contract is defined by `TranslatorApp.Models.WordModel`.

### POST /api/v3/Translation/SuggestedWords

Accepts text and a destination language and returns AI-generated word suggestions.

### GET /api/v1/Sound/DownloadSound

Downloads a sound URL for the client. MP3 data is returned directly; MP4 audio is converted to MP3 before it is returned.

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
