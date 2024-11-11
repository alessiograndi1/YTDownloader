using Microsoft.AspNetCore.Mvc;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos.Streams;
using YTDownloader.Models;

namespace YTDownloader.Controllers;

public class HomeController : Controller
{
    private readonly YoutubeClient _youtubeClient;

    public HomeController()
    {
        _youtubeClient = new YoutubeClient();
    }

    public IActionResult Index()
    {
        return View();
    }

    [HttpGet("search")]
    public async Task<IActionResult> GetSearchResults(string query)
    {
        // Ricerca risultati
        var results = await _youtubeClient.Search.GetVideosAsync(query);
        
        var videos = results
            .Take(9)
            .Select(video => new VideoResult
            {
                Title = video.Title,
                Url = video.Url
            });
        
        return PartialView("_SearchResults", videos);
    }

    [HttpPost("download")]
    public async Task<IActionResult> Download([FromForm] VideoRequest request)
    {
        if (string.IsNullOrEmpty(request.Query))
        {
            return BadRequest("L'URL del video non è valido");
        }

        try
        {
            // Ottieni informazioni sul video
            var videoId = YoutubeExplode.Videos.VideoId.Parse(request.Query);
            var video = await _youtubeClient.Videos.GetAsync(videoId);

            // Ottieni il flusso audio del video
            var streamManifest = await _youtubeClient.Videos.Streams.GetManifestAsync(videoId);
            var audioStreamInfo = streamManifest.GetAudioOnlyStreams().GetWithHighestBitrate();
            
            if (audioStreamInfo == null)
                return NotFound("Stream audio non trovato");

            // Cartella dei file
            var directoryPath = Path.Combine(Environment.CurrentDirectory, "Download");
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            // Percorso file
            var filePath = Path.Combine(directoryPath, $"{video.Title} - mdlr.mp3");
            
            // Scarica il flusso audio
            await using (var output = System.IO.File.Create(filePath))
            {
                await _youtubeClient.Videos.Streams.CopyToAsync(audioStreamInfo, output);
            }

            // Carica il file dal server e forzane il download nel browser
            var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var contentType = "audio/mpeg";  // Tipo MIME per MP3
            var fileDownloadName = $"{video.Title} - mdlr.mp3";

            // Forza il download con l'header Content-Disposition
            Response.Headers.AppendCommaSeparatedValues("Content-Disposition", $"attachment; filename={fileDownloadName}");

            return File(fileStream, contentType);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Errore: {ex.Message}");
        }
    }
}
