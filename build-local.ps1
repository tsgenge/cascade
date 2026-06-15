# Local build script for Cascade framework
# This script builds all projects and optionally packs them for local verification

param(
    [switch]$Clean,
    [switch]$SkipTests,
    [switch]$SkipPack,
    [switch]$UseProjectReferences,
    [string]$Configuration = "Release",
    [string]$Version = "1.0.0-local"
)

$ErrorActionPreference = "Stop"

Write-Host "🔨 Building Cascade Event Sourcing Framework" -ForegroundColor Cyan
Write-Host "Configuration: $Configuration" -ForegroundColor Gray
Write-Host "Version: $Version" -ForegroundColor Gray
if ($UseProjectReferences) {
    Write-Host "Mode: Project References (local development)" -ForegroundColor Gray
} else {
    Write-Host "Mode: Package References (CI simulation)" -ForegroundColor Gray
}
Write-Host ""

# Clean if requested
if ($Clean) {
    Write-Host "🧹 Cleaning solution..." -ForegroundColor Yellow
    dotnet clean --configuration $Configuration
    if (Test-Path "./artifacts") {
        Remove-Item -Recurse -Force "./artifacts"
    }
    if (Test-Path "./local-feed") {
        Remove-Item -Recurse -Force "./local-feed"
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

# Build solution (if using project references)
if ($UseProjectReferences) {
    Write-Host "🔨 Building solution..." -ForegroundColor Yellow
    dotnet build --configuration $Configuration --no-restore
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Build failed" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "✅ Build complete" -ForegroundColor Green
    Write-Host ""
}

# Run tests unless skipped
if (-not $SkipTests) {
    Write-Host "🧪 Running tests..." -ForegroundColor Yellow
    if ($UseProjectReferences) {
        dotnet test --configuration $Configuration --no-build --verbosity normal
    } else {
        dotnet test --configuration $Configuration --verbosity normal
    }
    if ($LASTEXITCODE -ne 0) {
        Write-Host "❌ Tests failed" -ForegroundColor Red
        exit $LASTEXITCODE
    }
    Write-Host "✅ Tests passed" -ForegroundColor Green
    Write-Host ""
}

# Pack unless skipped
if (-not $SkipPack) {
    Write-Host "📦 Packing NuGet packages..." -ForegroundColor Yellow
    
    if ($UseProjectReferences) {
        Write-Host "Using project references (local development mode)" -ForegroundColor Gray
    } else {
        Write-Host "Using package references (simulating CI build)" -ForegroundColor Gray
    }
    Write-Host ""
    
    # Create directories
    New-Item -ItemType Directory -Force -Path "./artifacts" | Out-Null
    New-Item -ItemType Directory -Force -Path "./local-feed" | Out-Null
    
    $localFeedPath = (Resolve-Path "./local-feed").Path
    
    # Add local feed as NuGet source (remove first if exists)
    $sourceName = "CascadeLocalFeed"
    dotnet nuget remove source $sourceName 2>$null
    dotnet nuget add source $localFeedPath --name $sourceName
    Write-Host "Local feed configured: $localFeedPath" -ForegroundColor Gray
    Write-Host ""
    
    # Define projects in dependency order (matching GitHub workflow)
    $projects = @(
        "src/shared/CascadeEsdm.SharedKernel.Abstractions/CascadeEsdm.SharedKernel.Abstractions.csproj",
        "src/shared/CascadeEsdm.SharedKernel/CascadeEsdm.SharedKernel.csproj",
        "src/write/CascadeEsdm.WriteModel.Abstractions/CascadeEsdm.WriteModel.Abstractions.csproj",
        "src/write/CascadeEsdm.WriteModel/CascadeEsdm.WriteModel.csproj",
        "src/read/CascadeEsdm.ReadModel.Abstractions/CascadeEsdm.ReadModel.Abstractions.csproj",
        "src/read/CascadeEsdm.ReadModel/CascadeEsdm.ReadModel.csproj",
        "src/infrastructure/CascadeEsdm.SignalR/CascadeEsdm.SignalR.csproj"
    )
    
    # Pack each project
    foreach ($project in $projects) {
        $projectName = [System.IO.Path]::GetFileNameWithoutExtension($project)
        Write-Host "📦 Packing $projectName..." -ForegroundColor Cyan
        
        $packArgs = @(
            "pack", $project,
            "--configuration", $Configuration,
            "--output", "./artifacts",
            "/p:Version=$Version"
        )
        
        if ($UseProjectReferences) {
            $packArgs += "--no-build"
        } else {
            $packArgs += "/p:UseProjectReferences=false"
            $packArgs += "/p:CascadeVersion=$Version"
        }
        
        & dotnet $packArgs
        
        if ($LASTEXITCODE -ne 0) {
            Write-Host "❌ Pack failed for $projectName" -ForegroundColor Red
            exit $LASTEXITCODE
        }
        
        # Copy to local feed for next packages to reference
        Copy-Item "./artifacts/*.nupkg" "./local-feed/" -Force -ErrorAction SilentlyContinue
        
        Write-Host "✅ $projectName packed successfully" -ForegroundColor Green
        Write-Host ""
    }
    
    Write-Host "✅ All packages created successfully" -ForegroundColor Green
    Write-Host ""
    Write-Host "📦 Packages created:" -ForegroundColor Cyan
    Get-ChildItem "./artifacts/*.nupkg" | ForEach-Object {
        $size = [math]::Round($_.Length / 1KB, 2)
        Write-Host "  - $($_.Name) ($size KB)" -ForegroundColor Gray
    }
    Write-Host ""
    Write-Host "💡 Local feed location: $localFeedPath" -ForegroundColor Yellow
    Write-Host "   To use these packages locally, add this source:" -ForegroundColor Gray
    Write-Host "   dotnet nuget add source $localFeedPath --name CascadeLocalFeed" -ForegroundColor Gray
}

Write-Host ""
Write-Host "✅ Build script completed successfully!" -ForegroundColor Green
