namespace Blake.BuildTools.Generator;

public static class ProjectFileBuilder
{
    public static async Task<int> InitProjectFile(string projectFilePath)
    {
        var projectContent = await File.ReadAllTextAsync(projectFilePath);

        if (!projectContent.Contains("<Project Sdk=\"Microsoft.NET.Sdk.BlazorWebAssembly\">"))
        {
            Console.WriteLine("Error: The specified project is not a Blazor WebAssembly app.");
            return 1;
        }

        // Check if the project already has Blake configured
        if (projectContent.Contains("<PackageReference Include=\"Blake.Types\""))
        {
            Console.WriteLine("Blake is already configured in this project.");
            return 0;
        }

        // Add Blake.BuildTools package reference

        var packageReference = "<PackageReference Include=\"Blake.Types\" Version=\"*\" />";

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

        const string blakeContentFolders = @"  <ItemGroup>
    <!-- Explicitly include generated .razor files -->
    <Content Include="".generated/**/*.razor"" />
    <Compile Include="".generated/**/*.cs"" />

    <!-- Remove template.razor files -->
    <Content Remove=""**/template.razor"" />
    <Compile Remove=""**/template.razor"" />
    <None Include=""**/template.razor"" />
  </ItemGroup>";

        projectContent = projectContent.Insert(projectEndIndex, $"{Environment.NewLine}{blakeContentFolders}{Environment.NewLine}");


        // Add the MS Build task to run the Blake.BuildTools generator
        if (!projectContent.Contains("<Target Name=\"BlakeBake\" BeforeTargets=\"BeforeBuild\">"))
        {
            const string blakeBuildToolsTask = @"
    <Target Name=""BlakeBake"" BeforeTargets=""BeforeBuild"">
      <Exec Command=""dotnet blake bake &quot;$(ProjectDir)&quot;"" />
    </Target>
";
            projectEndIndex = projectContent.LastIndexOf("</Project>", StringComparison.Ordinal);
            projectContent = projectContent.Insert(projectEndIndex, $"{Environment.NewLine}{blakeBuildToolsTask}{Environment.NewLine}");
        }

        // Write the updated content back to the project file
        await File.WriteAllTextAsync(projectFilePath, projectContent);

        return 0;
    }
}
