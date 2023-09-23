namespace Nexus.Spotify.Client.Models;

public class Track : Model
{
    public string Id { get; set; }
    public int Popularity { get; set; }
    public bool Explicit { get; set; }
    [JsonProperty("is_local")]
    public bool IsLocal { get; set; }
    public string Name { get; set; }
    /// <summary>
    /// Spotify 30 seconds audio preview.
    /// </summary>
    [JsonProperty("preview_url")]
    public string? PreviewUrl { get; set; }
    public string Uri { get; set; }
    public IEnumerable<Artist> Artists { get; set; }
    [JsonProperty("external_urls")]
    public IDictionary<string, string> Urls { get; set; }
    public IEnumerable<string> Genres { get; set; }
    [JsonProperty("duration_ms")]
    public int Duration { get; set; }
    public Album Album { get; set; }
    public Restrictions Restrictions { get; set; }

    public async Task DownloadPreviewAsync(Stream stream)
    {
        try
        {
            var str = await SpotifyClient.HttpClient.GetStreamAsync(PreviewUrl);
            await str.CopyToAsync(stream);
            stream.Position = 0;
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }
}

public class Restrictions
{
    public string Reason { get; set; }
}

public abstract class Model
{

    private SpotifyClient? spotifyClient;

    internal SpotifyClient SpotifyClient
    {
        get => spotifyClient ?? throw new ArgumentException("O modelo não foi carregado anteriormente");
        set => spotifyClient = value;
    }
}