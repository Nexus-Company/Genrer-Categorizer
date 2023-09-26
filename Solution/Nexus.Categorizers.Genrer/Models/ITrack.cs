namespace Nexus.Categorizers.Genrer.Models;

public interface ITrack
{
    public string Id { get; }
    public Stream GetPreview();
}