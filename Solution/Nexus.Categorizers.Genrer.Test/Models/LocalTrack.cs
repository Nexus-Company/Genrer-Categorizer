using Nexus.Categorizers.Genrer.Models;
using Nexus.Spotify.Client.Models;

namespace Nexus.Categorizers.Genrer.Test.Models;

public class LocalTrack : ITrack
{
    private readonly string _id;
    private readonly Stream _stream;
    private Task? downloadTask;
    public string Id { get => _id; }

    public Stream GetPreview()
    {
        if (downloadTask is not null)
        {
            downloadTask.Wait();
            downloadTask = null;
        }

        return _stream;
    }

    public LocalTrack(Track trk)
    {
        _id = trk.Id;

        string tempFile = Path.Combine(Path.GetTempPath(), "Music-Analizer");

        if (!Directory.Exists(tempFile))
            Directory.CreateDirectory(tempFile);

        tempFile = Path.Combine(tempFile, "Previews");

        if (!Directory.Exists(tempFile))
            Directory.CreateDirectory(tempFile);

        tempFile = Path.Combine(tempFile, $"{Id}.mp3");

        if (File.Exists(tempFile))
            _stream = new FileStream(tempFile, FileMode.Open, FileAccess.Read);
        else
        {
            _stream = new FileStream(tempFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);

            downloadTask = trk.DownloadPreviewAsync(_stream);
        }
    }
}