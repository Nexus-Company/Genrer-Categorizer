namespace Nexus.Spotify.Client.Models;

public class Album
{
    public IEnumerable<Image>? Images { get; set; }
    public string Name { get; set; }
    public string Id { get; set; }
    public IEnumerable<string> Genres { get; set; }
}