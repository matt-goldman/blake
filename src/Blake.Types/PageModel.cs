namespace Blake.Types;

public class PageModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    public string Title { get; set; } = "Untitled";

    public DateTime? Date { get; set; }

    public List<string> Tags { get; set; } = [];

    public string Description { get; set; } = string.Empty;

    public bool Draft { get; set; } = false;

    public string Slug { get; set; } = string.Empty; // Can be inferred from file name if not provided

    public string? Image { get; set; }

    public string? IconIdentifier { get; set; }

    public Dictionary<string, string> Metadata { get; set; } = [];
}
