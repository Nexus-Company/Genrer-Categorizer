namespace Nexus.Spotify.Client.Models;

public class Artist
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Label { get; set; }
    public int Popularity { get; set; }
    public IEnumerable<string> Genres { get; set; }
}
