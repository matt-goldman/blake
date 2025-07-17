using Blake.BuildTools.Utils;
using Blake.MarkdownParser;
using Markdig;
using Markdig.Prism;

namespace Blake.BuildTools.Generator;

internal static class SiteGenerator
{
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

    public static async Task BuildAsync(GenerationOptions? options = null)
    {
        options ??= new GenerationOptions
        {
            ProjectPath = Directory.GetCurrentDirectory(),
            OutputPath = Path.Combine(Directory.GetCurrentDirectory(), ".generated"),
            ContentFolders = []
        };

        Console.WriteLine($"🔧 Building site from project path: {options.ProjectPath}");
        Console.WriteLine($"📂 Output path: {options.OutputPath}");
        Console.WriteLine($"📁 Content folders: {string.Join(", ", options.ContentFolders)}");
        Console.WriteLine("🔎 Scanning content folders...");

        var allPageMetadata = new List<PageMetadata>();
        
        // iterate through all folders in the project path, find template.razor files
        if (!Directory.Exists(options.ProjectPath))
        {
            Console.WriteLine($"Error: Project path '{options.ProjectPath}' does not exist.");
            return;
        }
        if (!Directory.Exists(options.OutputPath))
        {
            Directory.CreateDirectory(options.OutputPath);
            Console.WriteLine($"✅ Created output directory: {options.OutputPath}");
        }
        
        if (options.ContentFolders.Length == 0)
        {
            Console.WriteLine("⚠️  No content folders specified. Will check every folder in the project path.");
            options.ContentFolders = Directory.GetDirectories(options.ProjectPath)
                .Select(Path.GetFileName)
                .Where(folder => 
                    folder != null &&
                    !folder.StartsWith('.') &&
                    !folder.Equals("obj", StringComparison.OrdinalIgnoreCase) &&
                    !folder.Equals("bin", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        foreach (var folder in options.ContentFolders)
        {
            if (folder == null) continue;
            
            var fullFolderPath = Path.Combine(options.ProjectPath, folder);
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

                var mdContent = await File.ReadAllTextAsync(mdPath);

                var frontmatter = FrontmatterHelper.ParseFrontmatter(mdContent, cleanedContent: out _);
                var metadata = FrontmatterHelper.MapToMetadata<PageMetadata>(frontmatter);

                metadata.Slug = slug;
                allPageMetadata.Add(metadata);

                var parsedContent = Markdown.ToHtml(mdContent, Pipeline);

                var generatedRazor = RazorPageBuilder.BuildRazorPage(templatePath, parsedContent, slug, metadata);

                var outputDir = Path.Combine(options.OutputPath, folder.ToLowerInvariant());
                Directory.CreateDirectory(outputDir);

                var outputPath = Path.Combine(outputDir, $"{fileName}.razor");
                await File.WriteAllTextAsync(outputPath, generatedRazor);

                Console.WriteLine($"✅ Generated page: {outputPath}");
            }
        }

        // Write content index
        ContentIndexBuilder.WriteIndex(options.OutputPath, allPageMetadata);
        Console.WriteLine($"✅ Generated content index in {options.OutputPath}");
    }

    public static async Task<int> InitAsync(string projectFile, bool? includeSampleContent = false)
    {
        var csprojResult = await ProjectFileBuilder.InitProjectFile(projectFile);

        if (csprojResult != 0)
        {
            Console.WriteLine("❌ Failed to initialize Blake in the project. Please check the project file.");
            return csprojResult;
        }

        // Add partial GeneratedContentIndex class
        var generatedIndexPath = Path.GetDirectoryName(projectFile) ?? string.Empty;
        ContentIndexBuilder.WriteIndexPartial(generatedIndexPath);

        // Add sample content to the Pages folder
        if (includeSampleContent == true)
        {
            await SampleContentBuilder.InitSampleContent(projectFile);
        }

        Console.WriteLine("✅ Blake has been configured successfully.");
        Console.WriteLine("Run 'blake bake' to generate the site content.");
        Console.WriteLine("Run 'dotnet run' to run your new Blake site.");
        Console.WriteLine("Refer to the documentation for further setup instructions.");
        
        return 0;
    }
}
