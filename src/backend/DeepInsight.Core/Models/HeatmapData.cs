namespace DeepInsight.Core.Models;

public class HeatmapData
{
    public Guid ProjectId { get; set; }
    public string PageUrl { get; set; } = string.Empty;
    public string HeatmapType { get; set; } = "click"; // click, scroll, attention
    public float XPct { get; set; }
    public float YPct { get; set; }
    public int Count { get; set; }
    public DateTime Date { get; set; }
}
