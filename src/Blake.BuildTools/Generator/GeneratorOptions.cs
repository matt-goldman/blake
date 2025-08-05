namespace Blake.BuildTools.Generator;

internal class GenerationOptions
{
    public string ProjectPath { get; init; } = default!;

    public string OutputPath { get; init; } = default!;

    public bool UseDefaultRenderers { get; init; } = true;

    public bool IncludeDrafts { get; init; } = false;

    public bool Clean { get; set; } = false;

    public string[] Arguments { get; init; } = [];
}
