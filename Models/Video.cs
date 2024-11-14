using YoutubeExplode.Common;

namespace YTDownloader.Models;

public class VideoRequest
{
    public string Query { get; set; } = null!;
}

public class VideoResult
{
    public string? Title { get; set; }
    public string? Url { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? Author { get; set; }
}