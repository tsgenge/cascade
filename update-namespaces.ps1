# Script to update all Cascade.* namespaces to CascadeEsdm.*

$ErrorActionPreference = "Stop"

Write-Host "Updating namespaces from Cascade.* to CascadeEsdm.*" -ForegroundColor Cyan

# Get all C# files in src directory
$files = Get-ChildItem -Path "src" -Filter "*.cs" -Recurse

$totalFiles = $files.Count
$updatedFiles = 0

foreach ($file in $files) {
    $content = Get-Content $file.FullName -Raw
    $originalContent = $content
    
    # Replace namespace declarations
    $content = $content -replace 'namespace Cascade\.', 'namespace CascadeEsdm.'
    
    # Replace using statements
    $content = $content -replace 'using Cascade\.', 'using CascadeEsdm.'
    
    if ($content -ne $originalContent) {
        Set-Content -Path $file.FullName -Value $content -NoNewline
        $updatedFiles++
        Write-Host "  ✓ Updated: $($file.FullName.Replace((Get-Location).Path, '.'))" -ForegroundColor Green
    }
}

Write-Host ""
Write-Host "✅ Updated $updatedFiles out of $totalFiles files" -ForegroundColor Green
