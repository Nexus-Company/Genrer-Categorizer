namespace Nexus.Spotify.Client.Models;

public class Playlist : Model
{
    public string Id { get; set; }

    public string Name { get; set; }

    public bool Public { get; set; }

    public string Description { get; set; }

    public IEnumerable<Image> Images { get; set; }

    public PlaylistTracks Tracks { get; set; }

    public async Task<IEnumerable<PlaylistTrackItem>> GetTracksAsync()
    {
        var request = SpotifyClient.CreateRequest(Tracks.href);
        var response = await SpotifyClient.HttpClient.SendAsync(request);

        string body = await response.Content.ReadAsStringAsync();

        var rst = JsonConvert.DeserializeObject<PlaylistTracksResult>(body)!;

        foreach (var item in rst.Items)
            item.Track.SpotifyClient = SpotifyClient;

        return rst.Items;
    }

    private class PlaylistTracksResult
    {
        public IEnumerable<PlaylistTrackItem> Items { get; set; }
    }
}

public class PlaylistTrackItem
{
    [JsonProperty("added_at")]
    public DateTime Added { get; set; }
    public Track Track { get; set; }
}

public class PlaylistTracks
{
    public string href { get; set; }
    public int Total { get; set; }
}