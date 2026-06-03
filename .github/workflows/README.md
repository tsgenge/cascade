# GitHub Actions CI/CD Workflows

This directory contains GitHub Actions workflows for validating pull requests and publishing the Cascade Event Sourcing framework to NuGet.

Branch model:
- **`develop`** → pre-release (alpha) packages
- **`master`** → release packages
- **Pull requests** → validated by the PR Validation workflow (build + test)

## Workflows

### `pr-validation.yml` (PR Validation)
Validates pull requests. Intended to be a required status check.

**Triggers:**
- Pull requests (any base branch)
- Manual workflow dispatch

**Jobs:**
1. **build-and-test**: Restores, builds the solution, and runs all tests

### `ci-cd.yml` (Main Pipeline)
Builds, tests, and publishes packages on pushes to the long-lived branches. It does **not** run on pull requests — that is handled by PR Validation.

**Triggers:**
- Push to `master` or `develop`
- Manual workflow dispatch

**Jobs:**
1. **build-and-test**: Builds the solution and runs all tests
2. **publish-prerelease**: Publishes pre-release packages on push to `develop`
3. **publish-release**: Publishes release packages on push to `master`

### `publish-packages.yml` (Reusable Workflow)
A reusable workflow (called via `workflow_call`) that handles packaging and publishing.

**Inputs:**
- `is-prerelease`: Boolean flag to determine if this is a pre-release build

**Version Strategy:**
- **Pre-release** (`develop`): `1.0.0-alpha.{build-number}+{short-sha}`
- **Release** (`master`):
  - If tagged: uses the git tag version (e.g., `v1.2.3` → `1.2.3`)
  - If not tagged: `1.0.{build-number}`

## Package Publishing Order

Packages are built and published in dependency order:

1. `CascadeEsdm.SharedKernel.Abstractions` (no dependencies)
2. `CascadeEsdm.SharedKernel` (depends on SharedKernel.Abstractions)
3. `CascadeEsdm.WriteModel.Abstractions` (depends on SharedKernel.Abstractions)
4. `CascadeEsdm.WriteModel` (depends on SharedKernel + WriteModel.Abstractions)
5. `CascadeEsdm.ReadModel.Abstractions` (no dependencies)
6. `CascadeEsdm.ReadModel` (depends on ReadModel.Abstractions)
7. `CascadeEsdm.EventExtractor` (dotnet tool, `net10.0` only)
8. `CascadeEsdm.AIContext`

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

### 3. Branches

- `develop` - For pre-release/alpha versions
- `master` - For stable releases

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

### Pre-release Publishing
```bash
git checkout develop
# Make changes
git commit -m "Test pre-release"
git push origin develop
```

This publishes packages like `1.0.0-alpha.42+abc1234` to NuGet.

### Release Publishing
```bash
git checkout master
git merge develop
git push origin master
```

This publishes packages like `1.0.42` to NuGet.

### Tagged Release
```bash
git checkout master
git tag v1.2.3
git push origin v1.2.3
```

This publishes packages with version `1.2.3` and creates a GitHub release.

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
dotnet pack src/shared/CascadeEsdm.SharedKernel.Abstractions/CascadeEsdm.SharedKernel.Abstractions.csproj \
  --configuration Release \
  --output ./artifacts \
  /p:Version=1.0.0-local \
  /p:UseProjectReferences=false \
  /p:CascadeVersion=1.0.0-local
```

Note: When building locally for development, omit the `/p:UseProjectReferences=false` parameter to use project references.
