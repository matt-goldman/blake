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

    public static async Task<int> InitAsync(string projectFile)
    {
        var projectContent = await File.ReadAllTextAsync(projectFile);
        
        if (!projectContent.Contains("<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\">"))
        {
            Console.WriteLine("Error: The specified project is not a Blazor WebAssembly app.");
            return 1;
        }
        
        // Check if the project already has Blake configured
        if (projectContent.Contains("<PackageReference Include=\"Blake.BuildTools\""))
        {
            Console.WriteLine("Blake is already configured in this project.");
            return 0;
        }
        
        // Add Blake.BuildTools package reference
        
        var packageReference = "<PackageReference Include=\"Blake.BuildTools\" Version=\"1.0.0\" />";
        
        // check for existing package references
        if (projectContent.Contains("<PackageReference"))
        {
            // Insert before the closing </ItemGroup> tag
            var itemGroupIndex = projectContent.LastIndexOf("</ItemGroup>", StringComparison.Ordinal);
            if (itemGroupIndex == -1)
            {
                Console.WriteLine("Error: Project file does not contain a valid ItemGroup.");
                return 1;
            }
            
            projectContent = projectContent.Insert(itemGroupIndex, $"{Environment.NewLine}    {packageReference}");
        }
        else
        {
            // Create a new ItemGroup if none exists
            projectContent += $"{Environment.NewLine}<ItemGroup>{Environment.NewLine}    {packageReference}{Environment.NewLine}</ItemGroup>";
        }
        
        // Add a custom ItemGroup for Blake content folders before the closing </Project> tag
        var projectEndIndex = projectContent.LastIndexOf("</Project>", StringComparison.Ordinal);
        if (projectEndIndex == -1)
        {
            Console.WriteLine("Error: Project file does not end with </Project>.");
            return 1;
        }

        const string blakeContentFolders = @"<ItemGroup>
      <!-- Explicitly include generated .razor files -->
      <Content Include="".generated/**/*.razor"" />
      <Compile Include="".generated/**/*.cs"" />

      <!-- Remove template.razor files -->
      <Content Remove=""**/template.razor"" />
      <Compile Remove=""**/template.razor"" />
      <None Include=""**/template.razor"" />
    </ItemGroup>";
        
        projectContent = projectContent.Insert(projectEndIndex, $"{Environment.NewLine}{blakeContentFolders}{Environment.NewLine}");
        
        
        // Write the updated content back to the project file
        await File.WriteAllTextAsync(projectFile, projectContent);
        
        // Add partial GeneratedContentIndex class
        const string generatedContentIndex = @"using Blake.Types;

namespace Generated;

public static partial class GeneratedContentIndex
{
    public static partial List<PageMetadata> GetPages();
}";
        var generatedIndexPath = Path.Combine(Path.GetDirectoryName(projectFile) ?? string.Empty, "GeneratedContentIndex.cs");
        await File.WriteAllTextAsync(generatedIndexPath, generatedContentIndex);
        
        // Add sample content to the Pages folder
        var pagesFolder = Path.Combine(Path.GetDirectoryName(projectFile) ?? string.Empty, "Pages");
        if (!Directory.Exists(pagesFolder))
        {
            Directory.CreateDirectory(pagesFolder);
        }
        
        var samplePagePath = Path.Combine(pagesFolder, "samplepage.md");

        if (!File.Exists(samplePagePath))
        {
            await File.WriteAllTextAsync(samplePagePath, SamplePageContent);
            Console.WriteLine($"✅ Sample page created at: {samplePagePath}");
        }
        else
        {
            Console.WriteLine($"⚠️  Sample page already exists at: {samplePagePath}");
        }
        
        // Add sample template
        var templatePath = Path.Combine(pagesFolder, "template.razor");
        if (!File.Exists(templatePath))
        {
            await File.WriteAllTextAsync(templatePath, SampleTemplate);
            Console.WriteLine($"✅ Sample template created at: {templatePath}");
        }
        else
        {
            Console.WriteLine($"⚠️  Sample template already exists at: {templatePath}");
        }
        
        // update the nav menu
        var navMenuPath = Path.Combine(Path.GetDirectoryName(projectFile) ?? string.Empty, "Layout", "NavMenu.razor");

        if (File.Exists(navMenuPath))
        {
            var navMenuContent = await File.ReadAllTextAsync(navMenuPath);
            if (navMenuContent.Contains("</nav>"))
            {
                // Insert the new menu item before the closing </nav> tag
                const string newMenuItem = """
                                           @foreach (var content in GeneratedContentIndex.GetPages())
                                           {
                                               <div class="nav-item px-3">
                                                   <NavLink class="nav-link" href="@(content.Slug)">
                                                       <span class="@(content.IconIdentifier)" aria-hidden="true"></span> @content.Title
                                                   </NavLink>
                                               </div>
                                           }
                                           """;
                
                var insertIndex = navMenuContent.LastIndexOf("</nav>", StringComparison.Ordinal);
                
                if (insertIndex != -1)
                {
                    navMenuContent = navMenuContent.Insert(insertIndex, $"{Environment.NewLine}{newMenuItem}{Environment.NewLine}");
                    await File.WriteAllTextAsync(navMenuPath, navMenuContent);
                    Console.WriteLine($"✅ Updated NavMenu.razor with dynamic content links.");
                }
                else
                {
                    Console.WriteLine("⚠️  Could not find </nav> tag in NavMenu.razor to insert dynamic content links.");
                }
            }
        }

        Console.WriteLine("✅ Blake has been configured successfully.");
        Console.WriteLine("Run 'blake bake' to generate the site content.");
        Console.WriteLine("Run 'dotnet run' to run your new Blake site.");
        Console.WriteLine("Refer to the documentation for further setup instructions.");
        
        return 0;
    }

    private const string SamplePageContent = """
                                             ---
                                             title: 'My first test page'
                                             date: 2025-07-16
                                             image: images/blake-logo.png
                                             tags: ["non-technical", "personal", "career", "community"]
                                             description: "Get to know the fundamentals of Blake, the static site generator."
                                             iconIdentifier: "bi bi-plus-square-fill-nav-menu"
                                             ---

                                             ## Hello world!

                                             Hello. This is a test page, generated by the Blake Blazor static site generator. Like it?

                                             ## FAQ

                                             1. Why?

                                             I...honestly can't remember!

                                             2. What's next?

                                             Templating system...other features...I forget. I have a roadmap though.

                                             3. How do I use it?

                                             Eventually it will be a standalone CLI tool, you'll be able to use it like this:

                                             ```bash
                                             blake init
                                             blake bake
                                             dotnet run
                                             ```

                                             ## Roadmap

                                             ```bash
                                             blake new --template my-template
                                             blake new -t a-different-template
                                             ```

                                             """;
    private const string SampleTemplate = """
                                                <PageTitle>@Title</PageTitle>
                                                
                                                <p>
                                                    Published on: @Published
                                                </p>
                                                
                                                <p>
                                                    @Description
                                                </p>
                                                
                                                @Body
                                                
                                                @code {
                                                
                                                }
                                                """;
}
