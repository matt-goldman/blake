using Blake.BuildTools.Utils;
using Blake.Types;

namespace Blake.BuildTools.Tests.Utils;


public class TestPageModel : PageModel
{
    public bool Pinned { get; set; }

    public string? AuthorTitle { get; set; }
}

public class FrontmatterHelperTests
{
    [Fact]
    public void Maps_Known_Properties()
    {
        var dict = new Dictionary<string, object>
        {
            ["Title"] = "Hello World",
            ["Pinned"] = "true",
            ["Date"] = "2023-01-01",
            ["Tags"] = new[] { "intro", "sample" }
        };

        var result = FrontmatterHelper.MapToMetadata<TestPageModel>(dict);

        Assert.Equal("Hello World", result.Title);
        Assert.True(result.Pinned);
        Assert.Equal(new DateTime(2023, 1, 1), result.Date);
        Assert.Equal(new[] { "intro", "sample" }, result.Tags);
    }

    [Fact]
    public void Stores_Unknown_Fields_In_Metadata()
    {
        var dict = new Dictionary<string, object>
        {
            ["Author"] = "Jane Doe",
            ["Category"] = "Tech"
        };

        var result = FrontmatterHelper.MapToMetadata<TestPageModel>(dict);

        Assert.Equal("Jane Doe", result.Metadata["Author"]);
        Assert.Equal("Tech", result.Metadata["Category"]);
    }

    [Fact]
    public void Handles_Invalid_Bool_Gracefully()
    {
        var dict = new Dictionary<string, object>
        {
            ["Pinned"] = "maybe"
        };

        var result = FrontmatterHelper.MapToMetadata<TestPageModel>(dict);

        Assert.False(result.Pinned); // default value
    }

    [Fact]
    public void Ignores_Unknown_Nulls()
    {
        var dict = new Dictionary<string, object>
        {
            ["Unknown"] = null
        };

        var result = FrontmatterHelper.MapToMetadata<TestPageModel>(dict);

        Assert.DoesNotContain("Unknown", result.Metadata);
    }

    [Fact]
    public void Missing_Fields_Dont_Break()
    {
        var dict = new Dictionary<string, object>();

        var result = FrontmatterHelper.MapToMetadata<TestPageModel>(dict);

        Assert.False(result.Pinned);
        Assert.Null(result.Date);
        Assert.Empty(result.Tags);
        Assert.Empty(result.Metadata);
        Assert.Equal("Untitled", result.Title);
        Assert.Null(result.AuthorTitle);
        Assert.Null(result.Image);
        Assert.Null(result.IconIdentifier);
        Assert.Empty(result.Slug);
        Assert.Empty(result.Description);
    }
}
