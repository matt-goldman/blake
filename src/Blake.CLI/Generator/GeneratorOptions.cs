namespace Blake.CLI.Generator;

public class GenerationOptions
{
    public string ProjectPath { get; set; } = default!;
    public string OutputPath { get; set; } = default!;
    public string[] ContentFolders { get; set; } = Array.Empty<string>();
}
