using Blake.CLI.Generator;
using Blake.CLI.Utils;

namespace Blake.CLI;

class Program
{
    static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Usage: blake build <path-to-blazor-project>");
            return 1;
        }

        var targetPath = args[0];
        if (!Directory.Exists(targetPath))
        {
            Console.WriteLine($"Error: Path '{targetPath}' does not exist.");
            return 1;
        }

        Console.WriteLine($"🛠  Starting build for: {targetPath}");

        var options = new GenerationOptions
        {
            ProjectPath = targetPath,
            OutputPath = Path.Combine(targetPath, ".generated"),
            ContentFolders = new[] { "Posts", "Pages" }, // default
        };

        var generator = new SiteGenerator(options);
        await generator.BuildAsync();

        Console.WriteLine("✅ Build completed successfully.");
        return 0;
    }
}
