using Microsoft.AspNetCore.Mvc;
using NAudio.Lame;
using NAudio.Wave;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Videos;
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
                Url = video.Url,
                Duration = video.Duration,
                Author = video.Author.ChannelTitle
            });
        
        return PartialView("_SearchResults", videos);
    }

    [HttpGet("download")]
    public async Task<IActionResult> Download(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return BadRequest("L'URL del video non è valido");
        }

        try
        {
            // Ottieni informazioni sul video
            var videoId = YoutubeExplode.Videos.VideoId.Parse(url);
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

            // Percorso file audio originale
            var tempFilePath = Path.Combine(directoryPath, $"{video.Title}_temp.{audioStreamInfo.Container}");
            
            // Scarica il flusso audio nel formato originale
            await using (var output = System.IO.File.Create(tempFilePath))
            {
                await _youtubeClient.Videos.Streams.CopyToAsync(audioStreamInfo, output);
            }

            // Percorso file MP3 convertito
            var mp3FilePath = Path.Combine(directoryPath, $"{video.Title} - mdlr.mp3");

            // Conversione a MP3 a 320 kbps
            using (var reader = new MediaFoundationReader(tempFilePath))
            using (var writer = new LameMP3FileWriter(mp3FilePath, reader.WaveFormat, 320))
            {
                reader.CopyTo(writer);
            }

            // Carica il file MP3 per il download
            var fileStream = new FileStream(mp3FilePath, FileMode.Open, FileAccess.Read);
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
