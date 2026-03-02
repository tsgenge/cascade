#!/bin/bash
# Local build script for Cascade framework (Linux/macOS)
# This script builds all projects using project references (default local behavior)

set -e

CLEAN=false
TEST=false
PACK=false
CONFIGURATION="Release"
VERSION="1.0.0-local"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --clean)
            CLEAN=true
            shift
            ;;
        --test)
            TEST=true
            shift
            ;;
        --pack)
            PACK=true
            shift
            ;;
        --configuration)
            CONFIGURATION="$2"
            shift 2
            ;;
        --version)
            VERSION="$2"
            shift 2
            ;;
        *)
            echo "Unknown option: $1"
            echo "Usage: $0 [--clean] [--test] [--pack] [--configuration <config>] [--version <version>]"
            exit 1
            ;;
    esac
done

echo "🔨 Building Cascade Event Sourcing Framework"
echo "Configuration: $CONFIGURATION"
echo "Version: $VERSION"
echo ""

# Clean if requested
if [ "$CLEAN" = true ]; then
    echo "🧹 Cleaning solution..."
    dotnet clean --configuration "$CONFIGURATION"
    rm -rf ./artifacts
    echo "✅ Clean complete"
    echo ""
fi

# Restore dependencies
echo "📦 Restoring dependencies..."
dotnet restore
echo "✅ Restore complete"
echo ""

# Build solution
echo "🔨 Building solution..."
dotnet build --configuration "$CONFIGURATION" --no-restore
echo "✅ Build complete"
echo ""

# Run tests if requested
if [ "$TEST" = true ]; then
    echo "🧪 Running tests..."
    dotnet test --configuration "$CONFIGURATION" --no-build --verbosity normal
    echo "✅ Tests passed"
    echo ""
fi

# Pack if requested
if [ "$PACK" = true ]; then
    echo "📦 Packing NuGet packages..."
    echo "Note: Using project references (local development mode)"
    echo ""
    
    mkdir -p ./artifacts
    
    projects=(
        "src/shared/Cascade.SharedKernel.Abstractions/Cascade.SharedKernel.Abstractions.csproj"
        "src/shared/Cascade.SharedKernel/Cascade.SharedKernel.csproj"
        "src/commands/Cascade.Commands.Abstractions/Cascade.Commands.Abstractions.csproj"
        "src/commands/Cascade.Commands/Cascade.Commands.csproj"
        "src/queries/Cascade.Views.Abstractions/Cascade.Views.Abstractions.csproj"
        "src/queries/Cascade.Views/Cascade.Views.csproj"
    )
    
    for project in "${projects[@]}"; do
        project_name=$(basename "$project")
        echo "  📦 Packing $project_name..."
        dotnet pack "$project" \
            --configuration "$CONFIGURATION" \
            --output ./artifacts \
            /p:Version="$VERSION" \
            --no-build
    done
    
    echo ""
    echo "✅ All packages created successfully"
    echo ""
    echo "📦 Packages created:"
    ls -lh ./artifacts/*.nupkg | awk '{print "  - " $9 " (" $5 ")"}'
fi

echo ""
echo "✅ Build script completed successfully!"
