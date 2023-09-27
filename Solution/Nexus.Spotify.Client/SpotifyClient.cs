global using Newtonsoft.Json;
using Nexus.Spotify.Client.Models;

namespace Nexus.Spotify.Client;

public class SpotifyClient : IDisposable
{
    const string SpotifyEndPoint = "https://api.spotify.com/v1";
    internal const string PlayerUrl = $"{SpotifyEndPoint}/me/player";
    internal HttpClient HttpClient { get; } = new();
    internal OAuthCredential Credential { get => _credential; }
    private readonly OAuthCredential _credential;

    public SpotifyClient(OAuthCredential credential)
    {
        _credential = credential;
    }

    public async Task<PlayerState> GetStatusAsync(CancellationToken? stoppingToken)
    {
        stoppingToken ??= CancellationToken.None;
        var request = new HttpRequestMessage()
        {
            RequestUri = new Uri(PlayerUrl)
        };
        request.Headers.Authorization = Credential!.GetHeader();

        var response = await HttpClient.SendAsync(request, stoppingToken.Value);

        string contentStr = await response.Content.ReadAsStringAsync(stoppingToken.Value);

        return LoadModel(JsonConvert.DeserializeObject<PlayerState>(contentStr)!);
    }

    public async Task<IEnumerable<Track>> GetQueueAsync(CancellationToken? stoppingToken = null)
    {
        stoppingToken ??= CancellationToken.None;
        var request = CreateRequest($"{PlayerUrl}/queue?");

        var response = await HttpClient.SendAsync(request, stoppingToken.Value);

        string state = await response.Content.ReadAsStringAsync(stoppingToken.Value);

        var rst = JsonConvert.DeserializeObject<QueueResult>(state)!;

        return rst.Queue;
    }

    public async Task AddToQueueAsync(string trackUri, string? deviceId, CancellationToken? stoppingToken = null)
    {
        stoppingToken ??= CancellationToken.None;

        string query = ToQueryString(new Dictionary<string, string>() {
            { "uri", $"spotify:track:{trackUri}"},
            { "device_id", deviceId}
        });

        var request = CreateRequest($"{PlayerUrl}/queue?{query}");

        request.Method = HttpMethod.Post;

        var response = await HttpClient.SendAsync(request, stoppingToken.Value);

        if (!response.IsSuccessStatusCode)
            throw new ArgumentException();
    }

    public async Task<IEnumerable<Track>> SearchAsync(string q, string market = "BR", CancellationToken? stoppingToken = null)
    {
        stoppingToken ??= CancellationToken.None;

        var query = ToQueryString(new Dictionary<string, string>()
        {
            { "q", q },
            { "market", market },
            { "type", "track" }
        });

        var request = CreateRequest($"{SpotifyEndPoint}/search?{query}");

        var response = await HttpClient.SendAsync(request, stoppingToken.Value);

        string state = await response.Content.ReadAsStringAsync(stoppingToken.Value);

        var rst = JsonConvert.DeserializeObject<SearchTracksResult>(state);

        return rst.Tracks.Items;
    }

    public async Task<Track?> GetTrackAsync(string id, string market = "BR", CancellationToken? stoppingToken = null)
    {
        stoppingToken ??= CancellationToken.None;
        var request = CreateRequest($"{SpotifyEndPoint}/tracks/{id}?market={market}");

        var response = await HttpClient.SendAsync(request, stoppingToken.Value);

        if (!response.IsSuccessStatusCode)
            return null;

        string state = await response.Content.ReadAsStringAsync(stoppingToken.Value);

        return LoadModel(JsonConvert.DeserializeObject<Track>(state)!);
    }

    public async Task<IEnumerable<Playlist>> GetUserPlaylistAsync(int per_page = 50, CancellationToken? stoppingToken = null)
    {
        stoppingToken ??= CancellationToken.None;
        var request = CreateRequest($"{SpotifyEndPoint}/me/playlists?limit={per_page}");

        var response = await HttpClient.SendAsync(request, stoppingToken.Value);

        string state = await response.Content.ReadAsStringAsync(stoppingToken.Value);

        var rst = JsonConvert.DeserializeObject<PlaylistsResult>(state)!;

        foreach (var item in rst.Items)
            LoadModel(item);

        return rst.Items;
    }

    internal HttpRequestMessage CreateRequest(string uri)
    {
        var request = new HttpRequestMessage()
        {
            RequestUri = new Uri(uri),
        };
        request.Headers.Authorization = Credential!.GetHeader();
        return request;
    }

    internal static string ToQueryString(Dictionary<string, string> parameters)
    {
        var keyValuePairs = new List<string>();
        foreach (var parameter in parameters)
        {
            keyValuePairs.Add($"{parameter.Key}={Uri.EscapeDataString(parameter.Value)}");
        }

        return string.Join("&", keyValuePairs);
    }

    #region Results
    private class QueueResult
    {
        public IEnumerable<Track> Queue { get; set; }
    }

    private class SearchTracksResult
    {
        public TracksResults Tracks { get; set; }

        public class TracksResults
        {
            public string Href { get; set; }
            public IEnumerable<Track> Items { get; set; }
        }
    }

    class PlaylistsResult
    {
        public IEnumerable<Playlist> Items { get; set; }
    }
    #endregion

    public void Dispose()
    {
        HttpClient.Dispose();
        GC.SuppressFinalize(this);
    }

    private T LoadModel<T>(T md) where T : Model
    {
        md.SpotifyClient = this;
        return md;
    }
}