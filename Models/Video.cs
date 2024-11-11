namespace YTDownloader.Models;

public class VideoRequest
{
    public string Query { get; set; } = null!;
}

public class VideoResult
{
    public string Title { get; set; } = null!;
    public string Url { get; set; } = null!;
}