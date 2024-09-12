# Translation App (for use by CopyWords App)

This project contains an Azure Function App that translates input strings using the [Azure Translator API](https://azure.microsoft.com/en-us/products/ai-services/ai-translator).

The function app is triggered by HTTP and acts primarily as a proxy to the Azure Translator API. However, it can be replaced with any other translation service such as ChatGPT or Google Translate if needed.

## Create Translator Resource

Follow the [official guide](https://learn.microsoft.com/en-us/azure/cognitive-services/translator/translator-overview) to create a Translator resource in Azure. You only need to create the resource and note down the key. There's no need to build a separate project to access it, as our Azure Function App will handle that.

## Create Azure Function App

Follow any guide on how to create a new Azure Function App. For example, you can use this [official Microsoft guide](https://learn.microsoft.com/en-us/azure/azure-functions/functions-create-function-app-portal?pivots=programming-language-csharp).

- For the **Authorization level**, select **Function** (you can change it to "Anonymous" later if needed). This means you will need to pass a secret (called a "code") in the URL to authenticate.

## Configure Local Environment

For testing under a debugger, you need to create two files:

### 1. `http-client.env.json`

This file will contain the URL for your function app.

Example:

```json
{
  "dev": {
    "HostAddress": "http://localhost:7014",
    "code": ""
  },
  "remote": {
    "HostAddress": "https://<your-function-app-name>.azurewebsites.net",
    "code": "<code-for-your-function>"
  }
}
```

### 2. `local.settings.json`

This file will contain all secrets required by the function.

Example:

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet-isolated",
    "APPINSIGHTS_INSTRUMENTATIONKEY": "<your-app-insights-key>",
    "TRANSLATIONS_APP_KEY": "<your-translator-app-key>",
    "TRANSLATIONS_APP_REGION": "<your-translator-app-region>"
  }
}
```

Once these files are set up, start the project in the debugger, open the `TestTranslateFunc.http` file, select the **Dev** environment, and send a test request. If everything is configured correctly, you will receive a response with the translation.

![Test in Visual Studio](https://raw.githubusercontent.com/evgenygunko/Translations/master/img/Test_From_VS.png)

## Configure Production Environment

1. Right-click on the solution and select **Publish**.
2. Follow the wizard to publish your Function App to Azure.

After publishing:

- Go to the Azure Portal.
- Select your Function App, then go to **Settings** -> **Configuration**.
- Add the following environment variables with your secrets:
  - `TRANSLATIONS_APP_KEY`
  - `TRANSLATIONS_APP_REGION`

Now you can invoke your function from Visual Studio or directly from the Azure Portal. In Visual Studio, open the `TestTranslateFunc.http` file, select the **remote** environment, and send a test request. If everything is set up correctly, you will receive a translation response.

## Using CI/CD

The repository includes an Azure DevOps pipeline that can be used to build and publish the Azure Function to Azure. You will need to create a new pipeline and configure a few variables, such as:

- Personal Access Tokens (PAT) for accessing the repository.
- Azure Service Connection for publishing the function app.
