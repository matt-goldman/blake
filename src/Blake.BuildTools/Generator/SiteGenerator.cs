using Blake.BuildTools.Utils;
using Blake.MarkdownParser;
using Markdig;
using Markdig.Renderers;
using Microsoft.Extensions.Logging;

namespace Blake.BuildTools.Generator;

internal static class SiteGenerator
{
    public static async Task BuildAsync(GenerationOptions? options = null, ILogger? logger = null)
    {
        options ??= new GenerationOptions
        {
            ProjectPath = Directory.GetCurrentDirectory(),
            OutputPath = Path.Combine(Directory.GetCurrentDirectory(), ".generated"),
        };

        logger?.LogInformation("🔧 Building site from project path: {OptionsProjectPath}", options.ProjectPath);
        logger?.LogInformation("📂 Output path: {OptionsOutputPath}", options.OutputPath);
        logger?.LogInformation("🔎 Scanning content folders...");

        
        // iterate through all folders in the project path, find template.razor files
        if (!Directory.Exists(options.ProjectPath))
        {
            logger?.LogError("Error: Project path '{OptionsProjectPath}' does not exist.", options.ProjectPath);
            return;
        }
        if (!Directory.Exists(options.OutputPath))
        {
            Directory.CreateDirectory(options.OutputPath);
            logger?.LogInformation("✅ Created output directory: {OptionsOutputPath}", options.OutputPath);
        }

        var context = await GetBlakeContext(options);

        var config = GetConfiguration(options.Arguments);

        // Load plugins
        var plugins = PluginLoader.LoadPlugins(options.ProjectPath, config);

        // Run BeforeBakeAsync for each plugin
        if (plugins.Count > 0)
        {
            logger?.LogDebug("🔌 Loaded {PluginsCount} plugin(s)", plugins.Count);
            foreach (var plugin in plugins)
            {
                try
                {
                    logger?.LogDebug("🔌 Running BeforeBakeAsync for plugin '{PluginPluginName}'", plugin.PluginName);
                    await plugin.Plugin.BeforeBakeAsync(context, logger);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning("⚠️  Error in plugin '{PluginPluginName}': {ExMessage}", plugin.PluginName, ex.Message);
                }
            }
        }
        else
        {
            logger?.LogDebug("No plugins loaded.");
        }

        await BakeContent(context, options, logger);

        // Run AfterBakeAsync for each plugin
        if (plugins.Count > 0)
        {
            foreach (var plugin in plugins)
            {
                try
                {
                    logger?.LogDebug("🔌 Running AfterBakeAsync for plugin '{PluginPluginName}'", plugin.PluginName);
                    await plugin.Plugin.AfterBakeAsync(context, logger);
                }
                catch (Exception ex)
                {
                    logger?.LogWarning("⚠️  Error in plugin '{PluginPluginName}': {ExMessage}", plugin.PluginName, ex.Message);
                }
            }
        }

        // write all generated Razor files to disk
        foreach (var generatedPage in context.GeneratedPages)
        {
            try
            {
                await File.WriteAllTextAsync(generatedPage.OutputPath, generatedPage.RazorHtml);
                logger?.LogDebug("✅ Successfully wrote page: {GeneratedPageOutputPath}", generatedPage.OutputPath);
            }
            catch (Exception ex)
            {
                logger?.LogWarning("⚠️  Error writing page '{PageSlug}': {ExMessage}", generatedPage.Page.Slug, ex.Message);
            }
        }

        // Write content index
        ContentIndexBuilder.WriteIndex(options.OutputPath, [.. context.GeneratedPages.Select(gp => gp.Page)]);
        logger?.LogDebug("✅ Generated content index in {OptionsOutputPath}", options.OutputPath);
    }

    public static async Task<int> InitAsync(string projectFile, bool? includeSampleContent = false, ILogger? logger = null)
    {
        var csprojResult = await ProjectFileBuilder.InitProjectFile(projectFile);

        if (csprojResult != 0)
        {
            logger?.LogError("Failed to initialize Blake in the project. Please check the project file.");
            return csprojResult;
        }

        // Add partial GeneratedContentIndex class
        var generatedIndexPath = Path.GetDirectoryName(projectFile) ?? string.Empty;
        ContentIndexBuilder.WriteIndexPartial(generatedIndexPath);

        var importsUpdated = true;

        // Add @using Blake.Types to _Imports.razor
        var importsPath = Path.Combine(Path.GetDirectoryName(projectFile) ?? string.Empty, "_Imports.razor");

        if (!File.Exists(importsPath))
        {
            importsUpdated = false;
        }
        else
        {
            var importsContent = await File.ReadAllTextAsync(importsPath);
            if (!importsContent.Contains("@using Blake.Types"))
            {
                var blakeImports = $"@using Blake.Types\n@using Blake.Generated\n";

                if (includeSampleContent == true)
                {
                    var fileName = Path.GetFileName(projectFile);
                    var projectComponentsNamespace = fileName.Replace(".csproj", ".Components");
                    blakeImports += $"@using {projectComponentsNamespace}\n";
                }

                await File.AppendAllTextAsync(importsPath, blakeImports);
            }
        }

        // Add sample content to the Pages folder
        if (includeSampleContent == true)
        {
            logger?.LogInformation("📝 Adding sample content to the project...");
            await SampleContentBuilder.InitSampleContent(projectFile);
            if (!importsUpdated)
            {
                logger?.LogWarning("⚠️  _Imports.razor was not found or updated. Sample content may not work as expected.");
            }
        }

        Console.WriteLine("✅ Blake has been configured successfully.");
        Console.WriteLine("Run 'blake bake' to generate the site content.");
        Console.WriteLine("Run 'dotnet run' to run your new Blake site.");
        Console.WriteLine("Or just run 'blake serve' to do both.");
        Console.WriteLine("Refer to the documentation for further setup instructions.");
        
        return 0;
    }
    
    public static async Task<int> NewSiteAsync(string newSiteName, string name, string? path = null, ILogger? logger = null)
    {
        // Initialize the new site
        // find the csproj file in the cloned directory
        var newSiteDirectory = path ?? Directory.GetCurrentDirectory();
        var templateCsprojPath = Directory.GetFiles(newSiteDirectory, "*.csproj", SearchOption.AllDirectories)
            .FirstOrDefault();
        
        if (templateCsprojPath == null)
        {
            logger?.LogError("Template Error: No .csproj file found in the cloned template directory.");
            return -1;
        }
        
        // rename the csproj file to the new site name
        var newCsprojPath = Path.Combine(Path.GetDirectoryName(templateCsprojPath) ?? string.Empty, $"{newSiteName}.csproj");
        if (File.Exists(newCsprojPath))
        {
            logger?.LogWarning("⚠️  A project file with the name '{NewSiteName}.csproj' already exists. It will be overwritten.", newSiteName);
            File.Delete(newCsprojPath);
        }
        
        File.Move(templateCsprojPath, newCsprojPath);
        
        // Remove spaces from template name
        var templatePlaceholderName = name.Replace(" ", string.Empty);
        var templatePlaceholder = "{{" + templatePlaceholderName + "}}";
        
        // replace "{{templatePlaceholder}}" in all files with newSiteName
        var fileList = Directory.GetFiles(newSiteDirectory, "*", SearchOption.AllDirectories);
        foreach (var file in fileList)
        {
            var fileContents = await File.ReadAllTextAsync(file);

            if (!fileContents.Contains(templatePlaceholder)) continue;
            
            fileContents = fileContents.Replace(templatePlaceholder, templatePlaceholderName);
            
            await File.WriteAllTextAsync(file, fileContents);
        }

        logger?.LogInformation("✅ Template {name} created as '{newSiteName}'.", name, newSiteName);
        return 0;
    }

    private static string GetConfiguration(string[] args)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            var arg = args[i];

            if (arg != "--configuration" && arg != "-c") continue;
            var value = args[i + 1];

            if (string.Equals(value, "debug", StringComparison.OrdinalIgnoreCase))
                return "Debug";

            if (string.Equals(value, "release", StringComparison.OrdinalIgnoreCase))
                return "Release";
        }

        return "Debug"; // default fallback
    }

    private static async Task<BlakeContext> GetBlakeContext(GenerationOptions options, ILogger? logger = null)
    {
        var context = new BlakeContext
        {
            ProjectPath = options.ProjectPath,
            Arguments = [.. options.Arguments],
            PipelineBuilder = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseFigures()
                .UseYamlFrontMatter()
                .UseBootstrap() // Safe to add to all projects, even if not using Bootstrap
                .UseImageCaptions()
                .SetupContainerRenderers(options.UseDefaultRenderers, useRazorContainers: true)
        };

        var folders = Directory.GetDirectories(options.ProjectPath)
            .Select(Path.GetFileName)
            .Where(folder =>
                folder != null &&
                !folder.StartsWith('.') &&
                !folder.Equals("obj", StringComparison.OrdinalIgnoreCase) &&
                !folder.Equals("bin", StringComparison.OrdinalIgnoreCase))
            .Select(f => f!)
            .ToList();

        var templateMappings = MapTemplates(folders, options.ProjectPath, null);
        
        // Pre-bake: Load existing markdown files into the context
        foreach (var mapping in templateMappings)
        {
            var fileName = Path.GetFileNameWithoutExtension(mapping.Key);
            var folder = Path.Combine(options.ProjectPath, Path.GetDirectoryName(mapping.Key) ?? string.Empty);
            
            var slug = $"/{Path.GetFileName(folder).ToLowerInvariant()}/{fileName.ToLowerInvariant()}";

            var mdContent = await File.ReadAllTextAsync(mapping.Key);

            if (string.IsNullOrWhiteSpace(mdContent))
            {
                logger?.LogWarning("⚠️  Skipping empty markdown file: {FileName} in {Folder}", fileName, folder);
                continue;
            }

            context.MarkdownPages.Add(new MarkdownPage(mapping.Key, mapping.Value, slug, mdContent));
        }
        
        return context;
    }

    private static async Task BakeContent(BlakeContext context, GenerationOptions options,  ILogger? logger = null)
    {
        // Bake: Process each markdown file and generate Razor pages
        var mdPipeline = context.PipelineBuilder.Build();

        await using var sw = new StringWriter();
        var renderer = new HtmlRenderer(sw);
        mdPipeline.Setup(renderer);

        foreach (var mdPage in context.MarkdownPages)
        { 
            sw.GetStringBuilder().Clear();
            var mdContent = mdPage.RawMarkdown;

            var frontmatter = FrontmatterHelper.ParseFrontmatter(mdContent, cleanedContent: out _);
            var page = FrontmatterHelper.MapToMetadata<PageModel>(frontmatter);

            var fileName = Path.GetFileNameWithoutExtension(mdPage.MdPath) ?? "index";
            var folder = Path.GetDirectoryName(mdPage.MdPath)?.Replace(options.ProjectPath, string.Empty).Trim(Path.DirectorySeparatorChar) ?? string.Empty;

            if (page.Draft && !options.IncludeDrafts)
            {
                logger?.LogInformation("⚠️  Skipping draft page: {FileName} in {Folder}", fileName, folder);
                continue;
            }

            page.Slug = mdPage.Slug;

            //var parsedContent = Markdown.ToHtml(mdContent, mdPipeline);
            // 🔄 Parse the markdown
            var document = Markdig.Parsers.MarkdownParser.Parse(mdContent, mdPipeline);

            // 🖋️ Render it
            renderer.Render(document);
            renderer.Writer.Flush();

            // 🔙 Get the rendered HTML
            var renderedHtml = sw.ToString();

            var generatedRazor = RazorPageBuilder.BuildRazorPage(mdPage.TemplatePath, renderedHtml, mdPage.Slug, page);

            var outputDir = Path.Combine(options.OutputPath, folder.ToLowerInvariant());
            Directory.CreateDirectory(outputDir);

            // create output filename - remove spaces or dashes, and convert to PascalCase instead
            // Razor filenames must be PascalCase and cannot contain spaces or dashes; this avoids enforcing this convention in markdown files
            var fileNameParts = fileName.Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries);
            var outputFileName = string.Join("", fileNameParts.Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant()));

            var outputPath = Path.Combine(outputDir, $"{outputFileName}.razor");
            
            logger?.LogInformation("✅ Generated page: {OutputPath}", outputPath);

            context.GeneratedPages.Add(new GeneratedPage(page, outputPath, generatedRazor));
        }
    }

    private static Dictionary<string, string> MapTemplates(
        IEnumerable<string> folders,
        string rootPath,
        string? cascadingTemplatePath,
        ILogger? logger = null)
    {
        Dictionary<string, string> templateMappings = [];
        
        foreach (var folder in folders)
        {
            var fullFolderPath = Path.Combine(rootPath, folder);
            if (!Directory.Exists(fullFolderPath))
            {
                
                logger?.LogDebug("⚠️  Skipping missing folder: {Folder}", folder);
                continue;
            }

            var localCascadingTemplatePath = Path.Combine(fullFolderPath, "cascading-template.razor");
            var localTemplatePath = Path.Combine(fullFolderPath, "template.razor");
            
            if (File.Exists(localCascadingTemplatePath) && File.Exists(localTemplatePath))
            {
                logger?.LogWarning("⚠️  Folder {FullFolderPath} contains both local and cascading templates. Skipping.", fullFolderPath);
                continue;
            }
            
            var cascadingPath = File.Exists(localCascadingTemplatePath) ? localCascadingTemplatePath : cascadingTemplatePath;
            
            var templatePath = File.Exists(localTemplatePath) ? localTemplatePath : cascadingPath;
            if (string.IsNullOrEmpty(templatePath))
            {
                logger?.LogDebug("⚠️  No template.razor found in {Folder}, skipping.", folder);
                continue;
            }

            var markdownFiles = Directory.GetFiles(fullFolderPath, "*.md");

            if (markdownFiles.Length == 0)
            {
                logger?.LogDebug("⚠️  No markdown files found in {Folder}, skipping.", folder);
                continue;
            }

            foreach (var mdPath in markdownFiles)
            {
                templateMappings.Add(mdPath, templatePath);
            }
            
            var children = Directory.GetDirectories(fullFolderPath)
                .Select(Path.GetFileName)
                .Where(child =>
                    child != null &&
                    !child.StartsWith('.') &&
                    !child.Equals("obj", StringComparison.OrdinalIgnoreCase) &&
                    !child.Equals("bin", StringComparison.OrdinalIgnoreCase))
                .Select(child => child!)
                .ToList();
            
            if (children.Count == 0) continue;
            
            var childMappings = MapTemplates(children, fullFolderPath, cascadingPath, logger);

            foreach (var child in childMappings)
            {
                templateMappings.Add(child.Key, child.Value);
            }
        }
        
        return templateMappings;
    }
}
