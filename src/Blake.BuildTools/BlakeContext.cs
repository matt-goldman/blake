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
    public required string ProjectName { get; init; }

    /// <summary>
    /// Gets the file system path to the project.
    /// </summary>
    public required string ProjectPath { get; init; }

    /// <summary>
    /// Gets the collection of arguments as key-value pairs.
    /// </summary>
    public required IReadOnlyDictionary<string, string> Arguments { get; init; }

    internal List<MarkdownPage> MarkdownPages { get; set; } = [];
    internal List<GeneratedPage> GeneratedPages { get; set; } = [];

    /// <summary>
    /// Retrieves a list of all markdown pages.
    /// </summary>
    /// <returns>A list of <see cref="MarkdownPage"/> objects representing the markdown pages. The list will be empty if no pages
    /// are available.</returns>
    /// <remarks>This method provides access to the markdown pages that have been processed or generated within the context of the Blake project.</remarks>
    public List<MarkdownPage> GetMarkdownPages() => [.. MarkdownPages];

    /// <summary>
    /// Retrieves a list of all generated pages.
    /// </summary>
    /// <returns>A list of <see cref="GeneratedPage"/> objects representing the generated pages.  The list is a copy of the
    /// internal collection and can be safely modified by the caller.</returns>
    /// <remarks>Will be empty if accessed from BeforeBake. This method provides access to the pages that have been processed and rendered into HTML format.</remarks>
    public List<GeneratedPage> GetGeneratedPages() => [.. GeneratedPages];

    /// <summary>
    /// Gets the <see cref="MarkdownPipelineBuilder"/> instance used to configure the Markdown processing pipeline.
    /// </summary>
    public required MarkdownPipelineBuilder Builder { get; init; } = new MarkdownPipelineBuilder();
}

public record MarkdownPage(string Path, string RawMarkdown);
public record GeneratedPage(PageModel Page, string RazorHtml);
