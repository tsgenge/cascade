# Quick Start Guide - NuGet Publishing

## TL;DR

1. **Add NuGet API Key** to GitHub Secrets as `NUGET_API_KEY`
2. **Push to `develop`** → Pre-release packages (e.g., `1.0.0-beta.42`)
3. **Push to `master`** → Release packages (e.g., `1.0.42`)
4. **Tag on `master`** → Versioned release (e.g., `v1.2.3` → `1.2.3`)

## Local Development

### Build and Test
```powershell
# Windows
.\build-local.ps1 -Test

# Linux/macOS
./build-local.sh --test
```

### Create Local Packages
```powershell
# Windows
.\build-local.ps1 -Pack -Version "1.0.0-local"

# Linux/macOS
./build-local.sh --pack --version "1.0.0-local"
```

## Publishing Workflow

### Pre-release (Beta)
```bash
git checkout develop
git add .
git commit -m "Add new feature"
git push origin develop
```
→ Publishes `1.0.0-beta.{build}+{sha}` to NuGet

### Release
```bash
git checkout master
git merge develop
git push origin master
```
→ Publishes `1.0.{build}` to NuGet

### Tagged Release
```bash
git checkout master
git tag v1.2.3
git push origin master
git push origin v1.2.3
```
→ Publishes `1.2.3` to NuGet + Creates GitHub Release

## How It Works

### Local Development (Default)
- Uses **project references** (`<ProjectReference>`)
- Changes in SharedKernel immediately available in Commands
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
Commands.Abstractions → Commands
    
Views.Abstractions → Views
```

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Packages not publishing | Check `NUGET_API_KEY` secret is set |
| Build fails | Verify tests pass locally first |
| Changes not reflected | Ensure not using `/p:UseProjectReferences=false` locally |
| Version conflict | Increment version or use different tag |

## Files Modified

- `Directory.Build.props` - Added NuGet metadata and conditional properties
- `*.csproj` - Added conditional references (project vs NuGet)
- `.github/workflows/ci-cd.yml` - Main CI/CD pipeline
- `.github/workflows/publish-packages.yml` - Reusable publish workflow

## Next Steps

1. Update package metadata in `Directory.Build.props`
2. Add `NUGET_API_KEY` to GitHub Secrets
3. Test by pushing to `develop` branch
4. Monitor GitHub Actions for build status
