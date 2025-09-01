using Markdig;

namespace Blake.BuildTools;

/// <summary>
/// Represents the context for a Blake project, providing access to project metadata, configuration arguments,  and
/// methods to retrieve associated pages.
/// </summary>
/// <remarks>This class is designed to encapsulate the essential information and functionality required to process
/// a Blake project. It includes project metadata such as the name and path, configuration arguments,  and methods to
/// retrieve markdown and generated pages.</remarks>
public class BlakeContext
{
    /// <summary>
    /// The name of the project.
    /// </summary>
    public string? ProjectName { get; set; }

    /// <summary>
    /// Gets the file system path to the project.
    /// </summary>
    public required string ProjectPath { get; init; }

    /// <summary>
    /// Gets the collection of arguments as key-value pairs.
    /// </summary>
    public required IReadOnlyList<string> Arguments { get; init; }

    /// <summary>
    /// Provides access to the pre-baked pages in the project.
    /// </summary>
    /// <remarks>Provided there is content in the project, should be available to BeforeBakeAsync.</remarks>
    public List<MarkdownPage> MarkdownPages { get; init; } = [];

    /// <summary>
    /// Provides access to the generated pages in the project.
    /// </summary>
    /// <remarks>Will not contain any entries until after the baking process is complete.</remarks>
    public List<GeneratedPage> GeneratedPages { get; init; } = [];

    /// <summary>
    /// Gets the <see cref="MarkdownPipelineBuilder"/> instance used to configure the Markdown processing pipeline.
    /// </summary>
    public required MarkdownPipelineBuilder PipelineBuilder { get; init; } = new MarkdownPipelineBuilder();
}

public record MarkdownPage(string MdPath, string TemplatePath, string Slug, string RawMarkdown);
public record GeneratedPage(PageModel Page, string OutputPath, string RazorHtml, string RawHtml);
