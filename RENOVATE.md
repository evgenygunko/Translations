# 🤖 Renovate Bot Integration

This repository is configured to use [Renovate Bot](https://www.mend.io/renovate/) for automated dependency management.

## What Does Renovate Do?

Renovate automatically:

- 📦 Scans your NuGet packages (`.csproj` files)
- 📦 Scans your npm packages (`package.json`)
- 🐳 Scans Docker base images (`Dockerfile`)
- 🔍 Checks for available updates
- 🔀 Creates pull requests with updates
- 🔒 Identifies security vulnerabilities
- 📊 Maintains a dependency dashboard

## Quick Start

### For Maintainers

1. **Install Renovate** (one-time setup)

   - Option A: Use [Mend Renovate App](https://www.mend.io/renovate/) for Azure DevOps
   - Option B: Use the Azure Pipeline (`azure-pipelines-renovate.yml`)
   - See [RENOVATE_SETUP.md](./RENOVATE_SETUP.md) for detailed instructions

2. **Merge the Onboarding PR**

   - Renovate will create an initial "Configure Renovate" PR
   - Review the configuration and merge it
   - Renovate will then start creating update PRs

3. **Monitor the Dashboard**
   - Check the "Dependency Dashboard" issue in your repo
   - It shows all pending and in-progress updates

### For Contributors

When you see a Renovate PR:

1. **Review the Changes**

   - Check what's being updated
   - Review release notes and changelogs (linked in PR)
   - Look for breaking changes

2. **Test Locally** (if needed)

   ```bash
   # Checkout the PR branch
   git fetch origin
   git checkout renovate/<branch-name>

   # Build and test
   dotnet restore
   dotnet build
   dotnet test

   # For npm packages
   yarn install
   yarn prettier:check
   ```

3. **Approve or Request Changes**
   - Approve if tests pass and changes look good
   - Request changes if issues found

## Configuration

The repository contains several Renovate configuration files:

- **`renovate.json`** - Main configuration (active)
- **`.renovaterc.json.example`** - Alternative config with detailed comments
- **`azure-pipelines-renovate.yml`** - Azure Pipeline for running Renovate
- **`RENOVATE_SETUP.md`** - Detailed setup instructions

### Current Configuration Highlights

- ✅ **Grouped Updates**: Similar packages are grouped together
- ✅ **Auto-merge**: Minor/patch updates for dev dependencies
- ✅ **Security First**: High-priority security updates
- ✅ **Weekly Schedule**: Runs every Monday at 6 AM UTC
- ✅ **Smart Grouping**:
  - Microsoft packages together
  - Test frameworks together
  - npm devDependencies together
  - Prettier tools together

## Package Update Strategy

### NuGet Packages

- **Minor/Patch**: Grouped together, auto-merge for test packages
- **Major**: Separate PRs, require review
- **Microsoft packages**: Grouped separately for easier review

### npm Packages

- **devDependencies**: Auto-merge minor/patch updates
- **dependencies**: Require review
- **Major updates**: Always require review

### Docker Images

- Base image updates grouped together
- Labeled with `docker` for easy filtering

## PR Labels

Renovate adds these labels to help categorize PRs:

- `dependencies` - All dependency updates
- `renovate` - Created by Renovate
- `major-update` - Major version updates
- `security` - Security-related updates
- `nuget` - NuGet package updates
- `npm` - npm package updates
- `docker` - Docker image updates

## Customizing Renovate

To customize Renovate behavior, edit `renovate.json`:

### Change Update Schedule

```json
{
  "schedule": ["before 6am every weekday"]
}
```

### Add Assignees/Reviewers

```json
{
  "assignees": ["your-azure-devops-username"],
  "reviewers": ["teammate-username"]
}
```

### Ignore Specific Packages

```json
{
  "ignoreDeps": ["package-name", "another-package"]
}
```

### Enable Auto-merge for More Packages

```json
{
  "packageRules": [
    {
      "matchPackageNames": ["specific-package"],
      "automerge": true
    }
  ]
}
```

## Troubleshooting

### Renovate Not Creating PRs?

1. Check if Renovate is properly configured in Azure DevOps
2. Verify Personal Access Token (if using pipeline)
3. Check `renovate.json` is valid JSON
4. Review Renovate logs in pipeline artifacts

### Too Many PRs?

1. Increase grouping in `renovate.json`
2. Reduce `prConcurrentLimit`
3. Adjust schedule to less frequent

### PRs Not Auto-merging?

1. Check if tests pass
2. Verify branch protection rules allow it
3. Check `automerge` configuration in `renovate.json`

## Resources

- 📚 [Renovate Documentation](https://docs.renovatebot.com/)
- 🔧 [Configuration Options](https://docs.renovatebot.com/configuration-options/)
- 🎯 [Azure DevOps Platform Guide](https://docs.renovatebot.com/modules/platform/azure/)
- 💬 [Renovate Discussions](https://github.com/renovatebot/renovate/discussions)

## Support

For issues or questions:

1. Check the [Renovate Setup Guide](./RENOVATE_SETUP.md)
2. Review [Renovate documentation](https://docs.renovatebot.com/)
3. Ask in [Renovate discussions](https://github.com/renovatebot/renovate/discussions)

---

**Last Updated**: 2025
**Renovate Version**: Latest
