# Local build script for Cascade framework
# This script builds all projects using project references (default local behavior)

param(
    [switch]$Clean,
    [switch]$Test,
    [switch]$Pack,
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0-local"
)

$ErrorActionPreference = "Stop"

Write-Host "🔨 Building Cascade Event Sourcing Framework" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Version: $Version" -ForegroundColor Gray
Write-Host ""

# Clean if requested
if ($Clean) {
    Write-Host "🧹 Cleaning solution..." -ForegroundColor Yellow
    dotnet clean --configuration $Configuration
    if (Test-Path "./artifacts") {
        Remove-Item -Recurse -Force "./artifacts"
    }
    Write-Host "✅ Clean complete" -ForegroundColor Green
    Write-Host ""
}

# Restore dependencies
Write-Host "📦 Restoring dependencies..." -ForegroundColor Yellow
dotnet restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Restore failed" -ForegroundColor Red
    exit $LASTEXITCODE
}
Write-Host "✅ Restore complete" -ForegroundColor Green
Write-Host ""

# Build solution
Write-Host "🔨 Building solution..." -ForegroundColor Yellow
dotnet build --configuration $Configuration --no-restore
if ($LASTEXITCODE -ne 0) {
    Write-Host "❌ Build failed" -ForegroundColor Red
    exit $LASTEXITCODE
}
Write-Host "✅ Build complete" -ForegroundColor Green
Write-Host ""

# Run tests if requested
if ($Test) {
    Write-Host "🧪 Running tests..." -ForegroundColor Yellow
    dotnet test --configuration $Configuration --no-build --verbosity normal
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Tests failed" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "✅ Tests passed" -ForegroundColor Green
    Write-Host ""
}

# Pack if requested
if ($Pack) {
    Write-Host "📦 Packing NuGet packages..." -ForegroundColor Yellow
    Write-Host "Note: Using project references (local development mode)" -ForegroundColor Gray
    Write-Host ""
    
    New-Item -ItemType Directory -Force -Path "./artifacts" | Out-Null
    
    $projects = @(
        "src/shared/Cascade.SharedKernel.Abstractions/Cascade.SharedKernel.Abstractions.csproj",
        "src/shared/Cascade.SharedKernel/Cascade.SharedKernel.csproj",
        "src/commands/Cascade.Commands.Abstractions/Cascade.Commands.Abstractions.csproj",
        "src/commands/Cascade.Commands/Cascade.Commands.csproj",
        "src/queries/Cascade.Views.Abstractions/Cascade.Views.Abstractions.csproj",
        "src/queries/Cascade.Views/Cascade.Views.csproj"
    )
    
    foreach ($project in $projects) {
        $projectName = Split-Path $project -Leaf
        Write-Host "  📦 Packing $projectName..." -ForegroundColor Cyan
        dotnet pack $project `
            --configuration $Configuration `
            --output ./artifacts `
            /p:Version=$Version `
            --no-build
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Pack failed for $projectName" -ForegroundColor Red
            exit $LASTEXITCODE
        }
    }
    
    Write-Host ""
    Write-Host "✅ All packages created successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "📦 Packages created:" -ForegroundColor Cyan
    Get-ChildItem "./artifacts/*.nupkg" | ForEach-Object {
        $size = [math]::Round($_.Length / 1KB, 2)
        Write-Host "  - $($_.Name) ($size KB)" -ForegroundColor Gray
    }
}

Write-Host ""
Write-Host "✅ Build script completed successfully!" -ForegroundColor Green
