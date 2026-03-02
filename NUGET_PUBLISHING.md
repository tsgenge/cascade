# NuGet Publishing Guide

This document explains the NuGet publishing setup for the Cascade Event Sourcing framework and how to work with it.

## Overview

The Cascade framework is split into multiple NuGet packages:

| Package | Description | Dependencies |
|---------|-------------|--------------|
| `Cascade.SharedKernel.Abstractions` | Core interfaces and abstractions | None |
| `Cascade.SharedKernel` | Core implementation | SharedKernel.Abstractions |
| `Cascade.Commands.Abstractions` | Command abstractions | SharedKernel.Abstractions |
| `Cascade.Commands` | Command implementation | SharedKernel, Commands.Abstractions |
| `Cascade.Views.Abstractions` | View/Query abstractions | None |
| `Cascade.Views` | View/Query implementation | Views.Abstractions |

## The Local Development vs NuGet Problem

### The Challenge

When developing a framework with inter-project dependencies, there's a conflict:

- **Local Development**: You want to use project references so changes in one project are immediately available in dependent projects
- **NuGet Publishing**: Published packages must reference other packages via NuGet, not project references

### The Solution

We use **conditional MSBuild properties** to switch between project references and NuGet package references based on the build context.

#### How It Works

1. **Directory.Build.props** defines a property `UseProjectReferences` that defaults to `true`
2. Each `.csproj` file has **two conditional ItemGroups**:
   - One with `<ProjectReference>` when `UseProjectReferences` is `true`
   - One with `<PackageReference>` when `UseProjectReferences` is `false`
3. During CI/CD builds, we set `/p:UseProjectReferences=false` to use NuGet references

#### Example from Cascade.Commands.csproj

```xml
<!-- Use project references locally -->
<ItemGroup Condition="'$(UseProjectReferences)' == 'true'">
  <ProjectReference Include="..\..\shared\Cascade.SharedKernel\Cascade.SharedKernel.csproj" />
  <ProjectReference Include="..\Cascade.Commands.Abstractions\Cascade.Commands.Abstractions.csproj" />
</ItemGroup>

<!-- Use NuGet package references in CI/CD -->
<ItemGroup Condition="'$(UseProjectReferences)' != 'true'">
  <PackageReference Include="Cascade.SharedKernel" Version="$(CascadeVersion)" />
  <PackageReference Include="Cascade.Commands.Abstractions" Version="$(CascadeVersion)" />
</ItemGroup>
```

## Local Development Workflow

### Normal Development (Default)

Just build and work as usual - project references are used automatically:

```bash
# Windows
.\build-local.ps1 -Test

# Linux/macOS
./build-local.sh --test
```

### Testing Package Creation Locally

To test how packages will be built in CI/CD (using NuGet references):

```bash
# Build packages with NuGet references
dotnet pack src/shared/Cascade.SharedKernel/Cascade.SharedKernel.csproj \
  --configuration Release \
  --output ./artifacts \
  /p:Version=1.0.0-test \
  /p:UseProjectReferences=false \
  /p:CascadeVersion=1.0.0-test
```

**Important**: You must build packages in dependency order when using `/p:UseProjectReferences=false`, or publish the dependencies to a local NuGet feed first.

## CI/CD Publishing Workflow

### Branch Strategy

- **`develop` branch** → Pre-release packages (e.g., `1.0.0-beta.42+abc1234`)
- **`master` branch** → Release packages (e.g., `1.0.42` or tagged version)

### Version Numbering

#### Pre-release (develop branch)
Format: `1.0.0-beta.{build-number}+{short-sha}`

Example: `1.0.0-beta.42+abc1234`

- `build-number`: Total commits on develop branch
- `short-sha`: Short commit hash for traceability

#### Release (master branch)

**With Git Tag:**
```bash
git tag v1.2.3
git push origin v1.2.3
```
Produces version: `1.2.3`

**Without Git Tag:**
Format: `1.0.{build-number}`

Example: `1.0.42`

- `build-number`: Total commits on master branch

### Publishing Process

The GitHub Actions workflow automatically:

1. **Builds** all projects with `/p:UseProjectReferences=false`
2. **Packs** packages in dependency order with the calculated version
3. **Pushes** to NuGet.org using the `NUGET_API_KEY` secret
4. **Creates** GitHub releases for tagged versions

### Dependency Order

Packages are built in this order to ensure dependencies are available:

1. `Cascade.SharedKernel.Abstractions`
2. `Cascade.SharedKernel`
3. `Cascade.Commands.Abstractions`
4. `Cascade.Views.Abstractions`
5. `Cascade.Commands`
6. `Cascade.Views`

## Setup Instructions

### 1. Configure NuGet API Key

1. Create an API key at https://www.nuget.org/account/apikeys
2. Add it as a GitHub repository secret named `NUGET_API_KEY`

### 2. Update Package Metadata

Edit `Directory.Build.props`:

```xml
<Authors>Your Name</Authors>
<Company>Your Company</Company>
<PackageProjectUrl>https://github.com/yourusername/cascade</PackageProjectUrl>
<RepositoryUrl>https://github.com/yourusername/cascade</RepositoryUrl>
```

### 3. Push to Trigger Publishing

**For pre-release:**
```bash
git checkout develop
git commit -m "Your changes"
git push origin develop
```

**For release:**
```bash
git checkout master
git merge develop
git push origin master
```

**For tagged release:**
```bash
git checkout master
git tag v1.2.3
git push origin master
git push origin v1.2.3
```

## Testing the Setup

### Verify Project References Work Locally

```bash
# Make a change in SharedKernel
# Build Commands project - it should see the change immediately
dotnet build src/commands/Cascade.Commands/Cascade.Commands.csproj
```

### Verify NuGet References Work in CI Mode

```bash
# Build with NuGet references (simulating CI)
dotnet pack src/commands/Cascade.Commands/Cascade.Commands.csproj \
  /p:UseProjectReferences=false \
  /p:CascadeVersion=1.0.0-test \
  --output ./test-artifacts
```

This should fail if the dependent NuGet packages aren't available, which is expected.

## Troubleshooting

### "Package 'Cascade.SharedKernel' not found" during local pack

This happens when you use `/p:UseProjectReferences=false` locally without publishing dependencies first. Solutions:

1. **Don't use** `/p:UseProjectReferences=false` for local development
2. **Or** set up a local NuGet feed and publish dependencies there first
3. **Or** build packages in dependency order

### Changes in one project not reflected in another

Make sure you're not accidentally setting `UseProjectReferences=false` in your local build.

### CI/CD build fails with missing packages

The workflow builds packages in dependency order, so this shouldn't happen. If it does:

1. Check the build order in `publish-packages.yml`
2. Verify all packages are being built before dependent packages
3. Check for typos in package names

### Version conflicts on NuGet

If you push the same version twice, NuGet will reject it. The workflow uses `--skip-duplicate` to handle this gracefully, but you should:

1. Use proper versioning (increment versions)
2. Use pre-release versions for testing
3. Use git tags for official releases

## Advanced: Local NuGet Feed

For testing the full NuGet workflow locally, you can set up a local feed:

```bash
# Create a local feed directory
mkdir C:\LocalNuGet

# Add it as a source
dotnet nuget add source C:\LocalNuGet -n LocalFeed

# Build and push packages to local feed
dotnet pack src/shared/Cascade.SharedKernel.Abstractions/Cascade.SharedKernel.Abstractions.csproj \
  -o C:\LocalNuGet \
  /p:Version=1.0.0-local

# Now dependent packages can find it
dotnet pack src/shared/Cascade.SharedKernel/Cascade.SharedKernel.csproj \
  -o C:\LocalNuGet \
  /p:Version=1.0.0-local \
  /p:UseProjectReferences=false \
  /p:CascadeVersion=1.0.0-local
```

## Best Practices

1. **Always work on develop branch** for new features
2. **Merge to master** only for releases
3. **Use git tags** for official version releases
4. **Test locally** before pushing to develop
5. **Monitor GitHub Actions** for build/publish status
6. **Keep versions consistent** across all packages in a release
