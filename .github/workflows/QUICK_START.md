# Quick Start Guide - NuGet Publishing

## TL;DR

1. **Add NuGet API Key** to GitHub Secrets as `NUGET_API_KEY`
2. **Open a PR** → PR Validation runs build + tests
3. **Push to `develop`** → Pre-release packages (e.g., `1.0.0-alpha.42+abc1234`)

> This repo uses a single `develop` branch (no gitflow/`master`).

## Local Development

### Build and Test
```bash
dotnet build --configuration Release
dotnet test --configuration Release
```

Or use the helper script (Windows, PowerShell):
```powershell
# Restore + build + test, skip packing
.\build-local.ps1 -SkipPack
```

### Create Local Packages
```powershell
# Windows (PowerShell)
.\build-local.ps1 -Version "1.0.0-local"
```

```bash
# Cross-platform: pack a single project
dotnet pack src/shared/CascadeEsdm.SharedKernel.Abstractions/CascadeEsdm.SharedKernel.Abstractions.csproj \
  --configuration Release \
  --output ./artifacts \
  /p:Version=1.0.0-local \
  /p:UseProjectReferences=false \
  /p:CascadeVersion=1.0.0-local
```

## Publishing Workflow

### Pre-release (alpha)
```bash
git checkout develop
git add .
git commit -m "Add new feature"
git push origin develop
```
→ Publishes `1.0.0-alpha.{build}+{sha}` to NuGet

## How It Works

### Local Development (Default)
- Uses **project references** (`<ProjectReference>`)
- Changes in SharedKernel immediately available in WriteModel
- No need to publish packages between changes

### CI/CD Builds
- Uses **NuGet package references** (`<PackageReference>`)
- Ensures published packages have correct dependencies
- Automatically switched via `/p:UseProjectReferences=false`

## Package Dependencies

```
SharedKernel.Abstractions (base)
    ↓
SharedKernel
    ↓
WriteModel.Abstractions → WriteModel

ReadModel.Abstractions → ReadModel

EventExtractor (dotnet tool)
AIContext
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Packages not publishing | Check `NUGET_API_KEY` secret is set |
| Build fails | Verify tests pass locally first |
| Changes not reflected | Ensure not using `/p:UseProjectReferences=false` locally |
| Version conflict | Increment version |

## Files

- `Directory.Build.props` - NuGet metadata and conditional properties
- `*.csproj` - Conditional references (project vs NuGet)
- `.github/workflows/pr-validation.yml` - PR validation (build + test)
- `.github/workflows/ci-cd.yml` - Main pipeline (build/test + prerelease publish on `develop`)
- `.github/workflows/publish-packages.yml` - Reusable publish workflow

## Next Steps

1. Update package metadata in `Directory.Build.props`
2. Add `NUGET_API_KEY` to GitHub Secrets
3. Test by pushing to `develop` branch
4. Monitor GitHub Actions for build status
