using Shared.Enums;

namespace Shared;

public class RankTask
{
    public string Id { get; set; } = string.Empty;
    public string TextKey { get; set; } = string.Empty;
    public string RankKey { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int RetryCount { get; set; }
}