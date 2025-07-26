<#
.SYNOPSIS
    Builds local NuGet packages for Blake projects.
.DESCRIPTION
    This script cleans the local NuGet cache for Blake packages and builds NuGet packages for specified projects.
    It ensures that the versioning is consistent and appends "-alpha" to the version if not already present.
.NOTES
    This script is intended for use in a development environment and may not be suitable for production use.
    Ensure that the .NET SDK is installed and available in your PATH.
.LINK
    https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-pack
.EXAMPLE
    .\Build-LocalPackages.ps1 -Version "1.0.0" -OutputPath "C:\LocalNuGetPackages"
    This command builds the local NuGet packages with version "1.0.0-alpha" and outputs them to the specified path.
#>



param (
    [string]$Version = "0.0.1-alpha",
    [string]$OutputPath = "$PSScriptRoot/local-packages"
)

$projects = @(
    "src/Blake.Types/Blake.Types.csproj",
    "src/Blake.MarkdownParser/Blake.MarkdownParser.csproj",
    "src/Blake.BuildTools/Blake.BuildTools.csproj",
    "src/Blake.CLI/Blake.CLI.csproj"
)

Write-Host "Cleaning local NuGet caches for Blake packages..."

foreach ($proj in $projects) {
    $name = [System.IO.Path]::GetFileNameWithoutExtension($proj)
    $cachePath = Join-Path -Path $HOME -ChildPath ".nuget/packages/$name"
    if (Test-Path $cachePath) {
        Remove-Item $cachePath -Recurse -Force
        Write-Host "Removed cache for $name"
    }
}

# Check if the output version ends with "-alpha", add it if not
if ($Version -notlike "*-alpha*") {
    $Version += "-alpha1"
}

Write-Host "Packing all projects with version $Version..."

foreach ($proj in $projects) {
    dotnet pack $proj -c Release -p:PackageVersion=$Version -o $OutputPath
}

Write-Host "`n✅ Build complete. Packages saved to: $OutputPath"
