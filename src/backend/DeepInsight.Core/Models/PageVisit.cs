namespace DeepInsight.Core.Models;

public class PageVisit
{
    public Guid Id { get; set; }
    public Guid SessionDbId { get; set; }
    public string Url { get; set; } = string.Empty;
    public string? Title { get; set; }
    public DateTime EnteredAt { get; set; }
    public DateTime? LeftAt { get; set; }

    public Session? Session { get; set; }
}
