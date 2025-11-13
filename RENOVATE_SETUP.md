# Renovate Bot Setup Guide for Azure DevOps

This guide explains how to integrate Renovate Bot with your Azure DevOps repository to automatically manage NuGet and npm package updates.

## Prerequisites

- Access to your Azure DevOps organization
- Repository: `https://dev.azure.com/evgenyg/CopyWords/_git/Translations`

## Setup Options

### Option 1: Using Mend Renovate App (Recommended)

1. **Install Mend Renovate App for Azure DevOps**

   - Visit the Azure DevOps Marketplace: https://marketplace.visualstudio.com/items?itemName=jyc.vsts-extensions-renovate-me
   - Or use the official Mend Renovate: https://www.mend.io/renovate/
   - Click "Get it free" and follow the installation steps

2. **Configure Repository Access**

   - Grant Renovate access to your `CopyWords` project
   - Ensure it has permissions to:
     - Read repository
     - Create pull requests
     - Write to branches

3. **Configure Renovate Settings in Azure DevOps**

   - Navigate to Project Settings → Service hooks
   - Add a webhook for Renovate (if required by your installation method)

4. **Verify Setup**
   - Once installed, Renovate will detect the `renovate.json` in your repository root
   - It will create an initial "Configure Renovate" PR
   - Review and merge this PR to activate Renovate

### Option 2: Self-Hosted Renovate

If you prefer to run Renovate on your own infrastructure:

1. **Set up a Personal Access Token (PAT)**

   - Go to Azure DevOps → User Settings → Personal Access Tokens
   - Create a new token with the following permissions:
     - Code: Read & Write
     - Pull Requests: Read & Write
   - Save the token securely

2. **Run Renovate via Docker**

   ```bash
   docker run -it --rm \
     -e RENOVATE_PLATFORM=azure \
     -e RENOVATE_ENDPOINT=https://dev.azure.com/evgenyg \
     -e RENOVATE_TOKEN=<your-pat-token> \
     -e LOG_LEVEL=debug \
     renovate/renovate:latest \
     evgenyg/CopyWords/Translations
   ```

3. **Set up as Azure Pipeline** (see `azure-pipelines-renovate.yml`)

### Option 3: Using Azure Pipelines (Automated)

Create a scheduled Azure Pipeline to run Renovate automatically:

1. **Create Pipeline**

   - Use the provided `azure-pipelines-renovate.yml` file
   - Set up the pipeline in Azure DevOps

2. **Configure Variables**

   - Add a secret variable `RENOVATE_TOKEN` with your PAT

3. **Schedule**
   - The pipeline is configured to run weekly (adjustable in the YAML)

## Configuration Details

### renovate.json Configuration

The `renovate.json` file in the repository root contains the following key features:

- **Dependency Dashboard**: Creates a single issue tracking all updates
- **Grouped Updates**:
  - All NuGet packages grouped together
  - All npm packages grouped together
- **Auto-merge**: Minor and patch updates for npm devDependencies
- **Security Alerts**: Enabled for vulnerability detection
- **Scheduling**: Updates run weekly on Mondays before 6am UTC
- **Labels**: Adds `dependencies` and `renovate` labels to PRs
- **Semantic Commits**: Uses conventional commit format

### Customization

Edit `renovate.json` to customize:

- **Schedule**: Change `"schedule": ["before 6am on Monday"]` to your preference
- **Assignees/Reviewers**: Add Azure DevOps usernames to automatically assign PRs
- **Auto-merge**: Enable/disable or adjust rules
- **Grouping**: Modify how dependencies are grouped in PRs
- **Labels**: Customize PR labels

## What Renovate Will Do

1. **Scan Dependencies**

   - NuGet packages in `.csproj` files
   - npm packages in `package.json`
   - Lock files (`package-lock.json`, `yarn.lock`, `packages.lock.json`)

2. **Create Pull Requests**

   - One PR per dependency group (or individual packages if not grouped)
   - Include release notes and changelogs
   - Show compatibility information

3. **Maintain Lock Files**

   - Keep lock files up to date
   - Ensure reproducible builds

4. **Security Alerts**
   - Prioritize security updates
   - Mark vulnerable dependencies

## Monitoring

- Check the Dependency Dashboard issue for overview
- Review PRs created by Renovate
- Monitor Azure DevOps notifications

## Troubleshooting

### Renovate Not Creating PRs

1. Verify Renovate has repository access
2. Check Azure DevOps permissions
3. Review Renovate logs (in app settings or pipeline logs)
4. Ensure `renovate.json` is valid JSON

### PRs Not Auto-merging

1. Check branch protection rules
2. Verify build/tests pass
3. Review `automerge` configuration in `renovate.json`

### Too Many PRs

1. Adjust `prConcurrentLimit` in `renovate.json`
2. Enable more grouping rules
3. Set stricter scheduling

## Resources

- [Renovate Documentation](https://docs.renovatebot.com/)
- [Azure DevOps Platform](https://docs.renovatebot.com/modules/platform/azure/)
- [Configuration Options](https://docs.renovatebot.com/configuration-options/)
- [Example Configs](https://docs.renovatebot.com/examples/)

## Support

For issues with:

- **Renovate Bot**: https://github.com/renovatebot/renovate/discussions
- **Azure DevOps Integration**: Check Renovate documentation for Azure platform
