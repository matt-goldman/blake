using Markdig;
using Markdig.Prism;
using Blake.MarkdownParser;

namespace Blake.CLI.Generator;

public class SiteGenerator
{
    private readonly GenerationOptions _options;

    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseCustomContainers()
        .UseFigures()
        .UseYamlFrontMatter()
        .UseBootstrap()
        .UsePrism(new PrismOptions
        {
            UseLineNumbers = true,
            UseCopyButton = true,
            UseLineHighlighting = true,
            UseLineDiff = true
        })
        .UseImageCaptions()
        .UseAncRenderers()
        .Build();

    public SiteGenerator(GenerationOptions options)
    {
        _options = options;
    }

    public async Task BuildAsync()
    {
        Console.WriteLine("🔎 Scanning content folders...");

        var allPageMetadata = new List<PageMetadata>();

        foreach (var folder in _options.ContentFolders)
        {
            var fullFolderPath = Path.Combine(_options.ProjectPath, folder);
            if (!Directory.Exists(fullFolderPath))
            {
                Console.WriteLine($"⚠️  Skipping missing folder: {folder}");
                continue;
            }

            var templatePath = Path.Combine(fullFolderPath, "template.razor");
            if (!File.Exists(templatePath))
            {
                Console.WriteLine($"⚠️  No template.razor found in {folder}, skipping.");
                continue;
            }

            var markdownFiles = Directory.GetFiles(fullFolderPath, "*.md");
            foreach (var mdPath in markdownFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(mdPath);
                var slug = $"/{folder.ToLowerInvariant()}/{fileName.ToLowerInvariant()}";
                var route = slug;

                var mdContent = await File.ReadAllTextAsync(mdPath);

                var sanitised = string.Empty;
                var frontmatter = FrontmatterHelper.ParseFrontmatter(mdContent, cleanedContent: out sanitised);
                var metadata = FrontmatterHelper.MapToMetadata<PageMetadata>(frontmatter);

                metadata.Slug = slug;
                allPageMetadata.Add(metadata);

                var parsedContent = Markdown.ToHtml(mdContent, Pipeline);

                var generatedRazor = RazorPageBuilder.BuildRazorPage(templatePath, parsedContent, route, metadata);

                var outputDir = Path.Combine(_options.OutputPath, folder.ToLowerInvariant());
                Directory.CreateDirectory(outputDir);

                var outputPath = Path.Combine(outputDir, $"{fileName}.razor");
                await File.WriteAllTextAsync(outputPath, generatedRazor);

                Console.WriteLine($"✅ Generated page: {outputPath}");
            }
        }

        // Write content index
        ContentIndexBuilder.WriteIndex(_options.OutputPath, allPageMetadata);
        Console.WriteLine($"✅ Generated content index in {_options.OutputPath}");
    }
}
