# Blake - Copilot Instructions

Blake is a Blazor-based static site generator that embraces **Occam's Razor** and **convention-over-configuration** principles. It allows developers to create static websites using familiar .NET technologies: Markdown files, Razor templates, and Blazor components.

## Development Guidelines

**Always search Microsoft documentation (MS Learn) when working with .NET, Windows, or Microsoft features, or APIs.** Use the `microsoft_docs_search` tool to find the most current information about capabilities, best practices, and implementation patterns before making changes.

## Project Philosophy

Blake is guided by simplicity and minimalism:

- **Minimal assumptions**: Use familiar .NET and Blazor patterns
- **Convention-over-configuration**: Folder structure determines behavior
- **Transparency**: Developers should understand their build process
- **Familiarity**: Leverage existing .NET/Blazor knowledge

**Core Principle**: "Bake your Blazor into beautiful static sites" with the fewest assumptions and maximum developer control.

## Architecture Overview

### Project Structure

```
Blake/
├── src/
│   ├── Blake.CLI/              # Command-line interface (`blake` tool)
│   ├── Blake.Types/            # Core types (PageModel, SiteTemplate)
│   ├── Blake.BuildTools/       # Build engine, plugin system, BlakeContext
│   └── Blake.MarkdownParser/   # Markdown parsing and rendering
├── tests/
│   └── Blake.BuildTools.Tests/ # Unit tests
├── TemplateRegistry.json      # Community template registry
└── Blake.sln                  # Solution file
```

### Key Components

1. **Blake.CLI**: Main entry point providing `bake`, `new`, `serve`, and `init` commands
2. **Blake.BuildTools**: Core build functionality, plugin architecture, and BlakeContext
3. **Blake.Types**: Shared types including `PageModel` for content metadata
4. **Blake.MarkdownParser**: Handles Markdown parsing with frontmatter support

### Build Flow

```plaintext
Markdown + template.razor → blake bake → .generated/*.razor → dotnet run → Blazor app
```

## Core Concepts

### Convention-Based Templates

- Each content folder (e.g., `/Posts`, `/Pages`) can contain a `template.razor` file
- Templates use standard Razor syntax with access to `PageModel` data
- Generated pages include proper `@page` routing directives
- Content is rendered using folder structure for URL generation

### Plugin System

Blake supports extensibility through the `IBlakePlugin` interface:

```csharp
public interface IBlakePlugin
{
    Task BeforeBakeAsync(BlakeContext context, ILogger? logger = null);
    Task AfterBakeAsync(BlakeContext context, ILogger? logger = null);
}
```

**Plugin Development Guidelines:**
- Plugins have access to `BlakeContext` containing project metadata and page collections
- Use `BeforeBakeAsync` for pre-processing (e.g., content validation)
- Use `AfterBakeAsync` for post-processing (e.g., adding metadata, external integrations)
- Example plugins: `BlakePlugin.ReadTime` (reading time calculation), `BlakePlugin.DocsRenderer` (TOC generation)

### Template Registry

Community templates are managed via `TemplateRegistry.json`:
- Templates include metadata: name, description, author, repository URL
- CLI supports `blake new --template <name>` and `blake new --list`
- Templates are Git repositories that can be cloned and customized

## Development Workflows

### Building the Project

**Note**: Blake targets .NET 9.0, ensure you have the correct SDK installed.

```bash
# Build the solution
dotnet build Blake.sln

# Run tests
dotnet test

# Pack NuGet packages locally (for development)
./Build-LocalPackages.ps1
```

### CLI Development

When working on the CLI (`Blake.CLI`):
- Entry point is `Program.cs` with command routing
- Commands include: `init`, `bake`, `serve`, `new`
- Use structured logging for user feedback
- Support verbosity levels and error handling
- Follow existing patterns for argument parsing

### Plugin Development

For new plugins:
1. Reference `Blake.BuildTools` for `IBlakePlugin` interface
2. Access project data through `BlakeContext`
3. Use `MarkdownPages` (before baking) or `GeneratedPages` (after baking)
4. Add metadata to `PageModel.Metadata` dictionary for template access
5. Follow existing plugin patterns (see ReadTime and DocsRenderer examples)

### Template Development

For new site templates:
1. Create a standard Blazor WASM project
2. Add Blake MSBuild integration to `.csproj`
3. Include `template.razor` files in content folders
4. Use YAML frontmatter in Markdown files for metadata
5. Reference the generated `GeneratedContentIndex` for navigation

## Code Conventions

### General Guidelines

- Follow standard .NET coding conventions and naming patterns
- Use nullable reference types throughout the codebase
- Prefer explicit error handling over exceptions where appropriate
- Include XML documentation for public APIs
- Use dependency injection patterns where applicable
- Do not make any formatting changes unless explicitly requested
- Do not include files in commits that contain only formatting or style changes, unless explicitly requested
- Do not downgrade .NET versions - Blake is built to work with .NET 9 and above

### Testing

- Unit tests are in `Blake.BuildTools.Tests`
- Test plugin functionality, build processes, and CLI commands
- Mock file system operations using abstractions where possible
- Include integration tests for end-to-end workflows

### Error Handling

- Use structured logging with appropriate log levels
- Provide actionable error messages to users
- Handle common scenarios gracefully (missing files, invalid configuration)
- Return appropriate exit codes from CLI operations

## Key Types and APIs

### PageModel

Core content metadata structure:
```csharp
public class PageModel
{
    public string Title { get; set; }
    public DateTime? Date { get; set; }
    public List<string> Tags { get; set; }
    public string Description { get; set; }
    public bool Draft { get; set; }
    public string Slug { get; set; }
    public Dictionary<string, string> Metadata { get; set; } // Custom properties
}
```

### BlakeContext

Plugin development context:
```csharp
public class BlakeContext
{
    public string ProjectPath { get; init; }
    public IReadOnlyList<string> Arguments { get; init; }
    public List<MarkdownPage> MarkdownPages { get; init; }      // Available in BeforeBakeAsync
    public List<GeneratedPage> GeneratedPages { get; init; }    // Available in AfterBakeAsync
    public MarkdownPipelineBuilder PipelineBuilder { get; init; } // Markdig pipeline
}
```

## Common Development Patterns

### Adding CLI Commands

1. Add command handling in `Program.Main()` switch statement
2. Create dedicated method (e.g., `BakeBlakeAsync`)
3. Use `GetLogger()` helper for consistent logging
4. Parse arguments and validate input
5. Call appropriate service methods from `Blake.BuildTools`

### Extending Markdown Processing

1. Add custom Markdig extensions to the pipeline via plugins
2. Use `BlakeContext.PipelineBuilder` to configure processing
3. Create custom renderers for special markup patterns
4. Process frontmatter through the existing YAML parsing

### Template Integration

1. Templates should include MSBuild targets to run `blake bake`
2. Use `GeneratedContentIndex` for site navigation
3. Access page metadata in templates via `PageModel`
4. Follow Blazor routing conventions for generated pages

## Integration Points

### MSBuild Integration

Templates include build targets that automatically run Blake during compilation:
```xml
<Target Name="BlakeBake" BeforeTargets="Build">
  <Exec Command="blake bake" WorkingDirectory="$(ProjectDir)" />
</Target>
```

### Blazor Integration

Generated files integrate seamlessly with Blazor:
- `.razor` pages with proper routing
- `GeneratedContentIndex.cs` with strongly-typed page access
- Support for Blazor components within Markdown content
- Standard Blazor development workflows (`dotnet run`, `dotnet watch`)

## Contributing Guidelines

### Before Making Changes

1. Understand the convention-over-configuration philosophy
2. Consider impact on existing templates and plugins
3. Ensure changes don't break the plugin API
4. Test with both CLI and MSBuild integration scenarios

### Pull Request Guidelines

1. Include tests for new functionality
2. Update documentation for user-facing changes
3. Consider backward compatibility for CLI commands
4. Test with existing community templates where possible

## Updating Instructions

**Important**: As Blake evolves, update these Copilot instructions to reflect:
- New CLI commands or options
- Plugin API changes
- Template system modifications  
- Additional development patterns
- New integration scenarios

Keep instructions current with the actual codebase to ensure Copilot suggestions remain accurate and helpful for contributors.

---

*These instructions help Copilot understand Blake's architecture, conventions, and development patterns to provide better code suggestions and assistance.*
