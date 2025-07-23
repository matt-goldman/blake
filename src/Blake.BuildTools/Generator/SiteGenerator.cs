using Blake.BuildTools.Utils;
using Blake.MarkdownParser;
using Markdig;

namespace Blake.BuildTools.Generator;

internal static class SiteGenerator
{
    public static async Task BuildAsync(GenerationOptions? options = null)
    {
        options ??= new GenerationOptions
        {
            ProjectPath = Directory.GetCurrentDirectory(),
            OutputPath = Path.Combine(Directory.GetCurrentDirectory(), ".generated"),
        };

        Console.WriteLine($"🔧 Building site from project path: {options.ProjectPath}");
        Console.WriteLine($"📂 Output path: {options.OutputPath}");
        Console.WriteLine("🔎 Scanning content folders...");

        
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
            .ToArray();

        // Pre-bake: Load existing markdown files into the context
        foreach (var folder in folders)
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

            if (markdownFiles.Length == 0)
            {
                Console.WriteLine($"⚠️  No markdown files found in {folder}, skipping.");
                continue;
            }

            foreach (var mdPath in markdownFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(mdPath);
                var slug = $"/{folder.ToLowerInvariant()}/{fileName.ToLowerInvariant()}";

                var mdContent = await File.ReadAllTextAsync(mdPath);

                if (string.IsNullOrWhiteSpace(mdContent))
                {
                    Console.WriteLine($"⚠️  Skipping empty markdown file: {fileName} in {folder}");
                    continue;
                }

                context.MarkdownPages.Add(new MarkdownPage(mdPath, templatePath, slug, mdContent));
            }
        }

        // Load plugins
        List<PluginContext> plugins = PluginLoader.LoadPlugins(options.ProjectPath);

        // Run BeforeBakeAsync for each plugin
        if (plugins.Count > 0)
        {
            Console.WriteLine($"🔌 Loaded {plugins.Count} plugin(s)");
            foreach (var plugin in plugins)
            {
                try
                {
                    await plugin.Plugin.BeforeBakeAsync(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Error in plugin '{plugin.PluginName}': {ex.Message}");
                }
            }
        }
        else
        {
            Console.WriteLine("No plugins found.");
        }


        // Bake: Process each markdown file and generate Razor pages
        var mdPipeline = context.PipelineBuilder.Build();

        foreach (var mdPage in context.MarkdownPages)
        { 
            var mdContent = mdPage.RawMarkdown;

            var frontmatter = FrontmatterHelper.ParseFrontmatter(mdContent, cleanedContent: out _);
            var page = FrontmatterHelper.MapToMetadata<PageModel>(frontmatter);

            var fileName = Path.GetFileNameWithoutExtension(mdPage.MdPath) ?? "index";
            var folder = Path.GetDirectoryName(mdPage.MdPath)?.Replace(options.ProjectPath, string.Empty).Trim(Path.DirectorySeparatorChar) ?? string.Empty;

            if (page.Draft && !options.IncludeDrafts)
            {
                Console.WriteLine($"⚠️  Skipping draft page: {fileName} in {folder}");
                continue;
            }

            page.Slug = mdPage.Slug;
            
            var parsedContent = Markdown.ToHtml(mdContent, mdPipeline);

            var generatedRazor = RazorPageBuilder.BuildRazorPage(mdPage.TemplatePath, parsedContent, mdPage.Slug, page);

            var outputDir = Path.Combine(options.OutputPath, folder.ToLowerInvariant());
            Directory.CreateDirectory(outputDir);

            // create output filename - remove spaces or dashes, and convert to PascalCase instead
            // Razor filenames must be PascalCase and cannot contain spaces or dashes; this avoids enforcing this convention in markdown files
            var fileNameParts = fileName.Split([' ', '-'], StringSplitOptions.RemoveEmptyEntries);
            var outputFileName = string.Join("", fileNameParts.Select(part => char.ToUpperInvariant(part[0]) + part.Substring(1).ToLowerInvariant()));

            var outputPath = Path.Combine(outputDir, $"{outputFileName}.razor");
            await File.WriteAllTextAsync(outputPath, generatedRazor);

            Console.WriteLine($"✅ Generated page: {outputPath}");

            context.GeneratedPages.Add(new GeneratedPage(page, generatedRazor));
        }

        // Run AfterBakeAsync for each plugin
        if (plugins.Count > 0)
        {
            foreach (var plugin in plugins)
            {
                try
                {
                    await plugin.Plugin.AfterBakeAsync(context);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️  Error in plugin '{plugin.PluginName}': {ex.Message}");
                }
            }
        }

        // Write content index
        ContentIndexBuilder.WriteIndex(options.OutputPath, [.. context.GeneratedPages.Select(gp => gp.Page)]);
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
            Console.WriteLine("📝 Adding sample content to the project...");
            await SampleContentBuilder.InitSampleContent(projectFile);
            if (!importsUpdated)
            {
                Console.WriteLine("⚠️  _Imports.razor was not found or updated. Sample content may not work as expected.");
            }
        }

        Console.WriteLine("✅ Blake has been configured successfully.");
        Console.WriteLine("Run 'blake bake' to generate the site content.");
        Console.WriteLine("Run 'dotnet run' to run your new Blake site.");
        Console.WriteLine("Or just run 'blake serve' to do both.");
        Console.WriteLine("Refer to the documentation for further setup instructions.");
        
        return 0;
    }
    
    public static async Task<int> NewSiteAsync(string newSiteName, string name, string? path = null)
    {
        // Initialize the new site
        // find the csproj file in the cloned directory
        var newSiteDirectory = path ?? Directory.GetCurrentDirectory();
        var templateCsprojPath = Directory.GetFiles(newSiteDirectory, "*.csproj", SearchOption.AllDirectories)
            .FirstOrDefault();
        
        if (templateCsprojPath == null)
        {
            Console.WriteLine("❌ Template Error: No .csproj file found in the cloned template directory.");
            return -1;
        }
        
        // rename the csproj file to the new site name
        var newCsprojPath = Path.Combine(Path.GetDirectoryName(templateCsprojPath) ?? string.Empty, $"{newSiteName}.csproj");
        if (File.Exists(newCsprojPath))
        {
            Console.WriteLine($"⚠️  A project file with the name '{newSiteName}.csproj' already exists. It will be overwritten.");
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

        Console.WriteLine($"✅ Template updated to '{newSiteName}'.");
        return 0;
    }
}
