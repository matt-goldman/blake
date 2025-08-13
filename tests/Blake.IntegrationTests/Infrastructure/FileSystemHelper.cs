using System.Diagnostics;

namespace Blake.IntegrationTests.Infrastructure;

/// <summary>
/// Helper utilities for file system operations and assertions in integration tests.
/// </summary>
public static class FileSystemHelper
{
    /// <summary>
    /// Creates a simple Blazor WASM project structure for testing.
    /// </summary>
    /// // TODO: Why not just do `dotnet new blazorwasm`?
    public static void CreateMinimalBlazorWasmProject(string projectPath, string projectName)
    {
        Directory.CreateDirectory(projectPath);

        // Create project file
        var csprojContent = $@"<Project Sdk=""Microsoft.NET.Sdk.BlazorWebAssembly"">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include=""Microsoft.AspNetCore.Components.WebAssembly"" Version=""9.0.7"" />
    <PackageReference Include=""Microsoft.AspNetCore.Components.WebAssembly.DevServer"" Version=""9.0.7"" PrivateAssets=""all"" />
  </ItemGroup>

</Project>";

        File.WriteAllText(Path.Combine(projectPath, $"{projectName}.csproj"), csprojContent);

        // Create wwwroot folder
        Directory.CreateDirectory(Path.Combine(projectPath, "wwwroot"));
        File.WriteAllText(Path.Combine(projectPath, "wwwroot", "index.html"), @"<!DOCTYPE html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>Test Blazor App</title>
</head>
<body>
    <div id=""app"">Loading...</div>
    <script src=""_framework/blazor.webassembly.js""></script>
</body>
</html>");

        // Create Program.cs
        File.WriteAllText(Path.Combine(projectPath, "Program.cs"), @"using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>(""#app"");
builder.RootComponents.Add<HeadOutlet>(""head::after"");

await builder.Build().RunAsync();");

        // Create App.razor
        File.WriteAllText(Path.Combine(projectPath, "App.razor"), @"<Router AppAssembly=""@typeof(App).Assembly"">
    <Found Context=""routeData"">
        <RouteView RouteData=""@routeData"" DefaultLayout=""@typeof(MainLayout)"" />
    </Found>
    <NotFound>
        <PageTitle>Not found</PageTitle>
        <LayoutView Layout=""@typeof(MainLayout)"">
            <p role=""alert"">Sorry, there's nothing at this address.</p>
        </LayoutView>
    </NotFound>
</Router>");

        // Create Shared folder and MainLayout
        Directory.CreateDirectory(Path.Combine(projectPath, "Shared"));
        File.WriteAllText(Path.Combine(projectPath, "Shared", "MainLayout.razor"), @"@inherits LayoutView

<div class=""page"">
    <div class=""main"">
        <article class=""content px-4"">
            @Body
        </article>
    </div>
</div>");

        // Create Pages folder
        Directory.CreateDirectory(Path.Combine(projectPath, "Pages"));
        File.WriteAllText(Path.Combine(projectPath, "Pages", "Index.razor"), @"@page ""/""

<PageTitle>Home</PageTitle>

<h1>Hello, world!</h1>");
    }

    /// <summary>
    /// Creates a Blazor WASM project using the dotnet CLI template.
    /// </summary>
    public static async Task CreateBlazorWasmProjectAsync(string projectPath, string projectName)
    {
        Directory.CreateDirectory(projectPath);
        
        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            Arguments = $"new blazorwasm -o \"{projectPath}\" -n \"{projectName}\" --framework net9.0",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            var error = await process.StandardError.ReadToEndAsync();
            throw new Exception($"Failed to create Blazor WASM project: {error}");
        }
    }

    /// <summary>
    /// Creates a markdown file with frontmatter.
    /// </summary>
    public static void CreateMarkdownFile(string filePath, string title, string content, Dictionary<string, object>? frontmatter = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);

        var fm = frontmatter ?? new Dictionary<string, object>();
        if (!fm.ContainsKey("title"))
            fm["title"] = title;

        var frontmatterYaml = string.Join(Environment.NewLine, fm.Select(kvp => $"{kvp.Key}: {FormatYamlValue(kvp.Value)}"));
        
        var markdownContent = $@"---
{frontmatterYaml}
---

{content}";

        File.WriteAllText(filePath, markdownContent);
    }

    /// <summary>
    /// Creates a Razor template file for Blake.
    /// </summary>
    public static void CreateRazorTemplate(string filePath, string content)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
        File.WriteAllText(filePath, content);
    }

    /// <summary>
    /// Asserts that a directory exists.
    /// </summary>
    public static void AssertDirectoryExists(string path, string? message = null)
    {
        Assert.True(Directory.Exists(path), message ?? $"Expected directory to exist: {path}");
    }

    /// <summary>
    /// Asserts that a directory does not exist.
    /// </summary>
    public static void AssertDirectoryNotExists(string path, string? message = null)
    {
        Assert.False(Directory.Exists(path), message ?? $"Expected directory to not exist: {path}");
    }

    /// <summary>
    /// Asserts that a file exists.
    /// </summary>
    public static void AssertFileExists(string path, string? message = null)
    {
        Assert.True(File.Exists(path), message ?? $"Expected file to exist: {path}");
    }

    /// <summary>
    /// Asserts that a file does not exist.
    /// </summary>
    public static void AssertFileNotExists(string path, string? message = null)
    {
        Assert.False(File.Exists(path), message ?? $"Expected file to not exist: {path}");
    }

    /// <summary>
    /// Asserts that a file contains specific text.
    /// </summary>
    public static void AssertFileContains(string filePath, string expectedText, string? message = null)
    {
        AssertFileExists(filePath);
        var content = File.ReadAllText(filePath);
        Assert.Contains(expectedText, content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Asserts that a file does not contain specific text.
    /// </summary>
    public static void AssertFileNotContains(string filePath, string unexpectedText, string? message = null)
    {
        if (!File.Exists(filePath)) return; // If file doesn't exist, it certainly doesn't contain the text
        
        var content = File.ReadAllText(filePath);
        Assert.DoesNotContain(unexpectedText, content, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets all files in a directory matching a pattern.
    /// </summary>
    public static IEnumerable<string> GetFiles(string directory, string pattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly)
    {
        if (!Directory.Exists(directory))
            return [];

        return Directory.GetFiles(directory, pattern, searchOption);
    }

    /// <summary>
    /// Formats a value for YAML frontmatter.
    /// </summary>
    private static string FormatYamlValue(object value)
    {
        return value switch
        {
            string s => s.Contains(' ') ? $"\"{s}\"" : s,
            DateTime dt => dt.ToString("yyyy-MM-dd"),
            bool b => b.ToString().ToLower(),
            IEnumerable<string> list => $"[{string.Join(", ", list.Select(i => $"\"{i}\""))}]",
            _ => value.ToString() ?? ""
        };
    }
}