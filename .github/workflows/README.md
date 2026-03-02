# GitHub Actions CI/CD Workflows

This directory contains GitHub Actions workflows for building, testing, and publishing the Cascade Event Sourcing framework to NuGet.

## Workflows

### `ci-cd.yml` (Main Pipeline)
The main CI/CD pipeline that orchestrates the entire build and publish process.

**Triggers:**
- Push to `master` or `develop` branches
- Pull requests to `master` or `develop` branches
- Manual workflow dispatch

**Jobs:**
1. **build-and-test**: Builds the solution and runs all tests
2. **publish-prerelease**: Publishes pre-release packages when pushing to `develop`
3. **publish-release**: Publishes release packages when pushing to `master`

### `publish-packages.yml` (Reusable Workflow)
A reusable workflow that handles the packaging and publishing logic.

**Inputs:**
- `is-prerelease`: Boolean flag to determine if this is a pre-release build

**Version Strategy:**
- **Pre-release** (develop branch): `1.0.0-beta.{build-number}+{short-sha}`
- **Release** (master branch): 
  - If tagged: Uses the git tag version (e.g., `v1.2.3` → `1.2.3`)
  - If not tagged: `1.0.{build-number}`

## Package Publishing Order

Packages are built and published in dependency order:

1. `Cascade.SharedKernel.Abstractions` (no dependencies)
2. `Cascade.SharedKernel` (depends on SharedKernel.Abstractions)
3. `Cascade.Commands.Abstractions` (depends on SharedKernel.Abstractions)
4. `Cascade.Commands` (depends on SharedKernel + Commands.Abstractions)
5. `Cascade.Views.Abstractions` (no dependencies)
6. `Cascade.Views` (depends on Views.Abstractions)

## Setup Instructions

### 1. Configure NuGet API Key

Add your NuGet API key as a repository secret:

1. Go to your repository on GitHub
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Name: `NUGET_API_KEY`
5. Value: Your NuGet API key from https://www.nuget.org/account/apikeys

### 2. Update Package Metadata

Edit `Directory.Build.props` in the repository root to update:
- `<Authors>` - Your name or organization
- `<Company>` - Your company name
- `<PackageProjectUrl>` - Your GitHub repository URL
- `<RepositoryUrl>` - Your GitHub repository URL

### 3. Create Branches

Ensure you have the required branches:
- `master` - For stable releases
- `develop` - For pre-release/beta versions

## Local Development vs CI/CD

The solution uses conditional MSBuild properties to handle dependencies differently in local development vs CI/CD:

### Local Development (Default)
- Uses **project references** (`<ProjectReference>`)
- Allows immediate changes across projects without publishing
- Set automatically when building locally

### CI/CD Builds
- Uses **NuGet package references** (`<PackageReference>`)
- Ensures published packages have correct NuGet dependencies
- Enabled by setting `/p:UseProjectReferences=false`

This is controlled by the `UseProjectReferences` property in `Directory.Build.props`.

## Testing the Workflow

### Test Pre-release Publishing
```bash
git checkout develop
# Make changes
git commit -m "Test pre-release"
git push origin develop
```

This will trigger a build and publish packages like `1.0.0-beta.42+abc1234` to NuGet.

### Test Release Publishing
```bash
git checkout master
git merge develop
git push origin master
```

This will trigger a build and publish packages like `1.0.42` to NuGet.

### Test with Git Tags
```bash
git checkout master
git tag v1.2.3
git push origin v1.2.3
```

This will publish packages with version `1.2.3` and create a GitHub release.

## Troubleshooting

### Packages not publishing
- Verify `NUGET_API_KEY` secret is set correctly
- Check the Actions tab for error messages
- Ensure your NuGet API key has push permissions

### Version conflicts
- The workflow uses `--skip-duplicate` to avoid errors when re-pushing the same version
- If you need to republish, increment the version or use a different tag

### Build failures
- Check that all tests pass locally first
- Verify .NET 10.0 SDK is available (update `DOTNET_VERSION` if needed)
- Review the build logs in the Actions tab

## Manual Package Creation

To manually create packages locally (for testing):

```bash
# Build with NuGet package references
dotnet pack src/shared/Cascade.SharedKernel.Abstractions/Cascade.SharedKernel.Abstractions.csproj \
  --configuration Release \
  --output ./artifacts \
  /p:Version=1.0.0-local \
  /p:UseProjectReferences=false \
  /p:CascadeVersion=1.0.0-local
```

Note: When building locally for development, omit the `/p:UseProjectReferences=false` parameter to use project references.
