namespace Blake.BuildTools.Generator;

public class GenerationOptions
{
    public string ProjectPath { get; set; } = default!;

    public string OutputPath { get; set; } = default!;

    public bool UseDefaultRenderers { get; set; } = true;

    public bool IncludeDrafts { get; set; } = false;
}
