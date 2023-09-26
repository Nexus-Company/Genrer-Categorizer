using Nexus.Categorizers.Genrer.Models;
using Nexus.Spotify.Client.Models;

namespace Nexus.Categorizers.Genrer.Analizer;

public class MusicAnalizer : MusicAnalizerBase
{
    private protected MusicAnalizer(GenreConvert genres, Stream str)
        : base(genres, str)
    {
    }

    public async Task<string[]> GetGenreAsync(Track track)
    {
        var str = await DownloadAsync(track);

        var mfccs = CalculateMFCCs(str);

        bool[] results = _machine.Decide(mfccs);

        List<string> result = new();

        for (int i = 0; i < results.Length; i++)
        {
            if (results[i])
                result.Add(genreConvert[i]);
        }

        return result.ToArray();
    }

    public static new MusicAnalizer Load(string file)
    {
        var str = GetLoader(file, out GenreConvert genres);

        return new MusicAnalizer(genres, str);
    }
}