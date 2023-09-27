using Nexus.Categorizers.Genrer.Models;

namespace Nexus.Categorizers.Genrer.Analizer;

public partial class MusicTrainner
{
    private struct AddTrainning
    {
        public AddTrainning(ITrack track, string[] genres)
        {
            Track = track ?? throw new ArgumentNullException(nameof(track));
            Genres = genres ?? throw new ArgumentNullException(nameof(genres));
        }

        public ITrack Track { get; set; }
        public string[] Genres { get; set; }
    }
}
