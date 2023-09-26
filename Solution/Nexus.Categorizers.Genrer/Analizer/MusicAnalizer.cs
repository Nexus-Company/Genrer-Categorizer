using Nexus.Categorizers.Genrer.Models;

namespace Nexus.Categorizers.Genrer.Analizer;

public class MusicAnalizer : MusicAnalizerBase
{
    private protected MusicAnalizer(GenreConvert genres, Stream str)
        : base(genres, str)
    {
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="track"></param>
    /// <returns></returns>
    public async Task<string[]> GetGenreAsync(ITrack track)
    {
        var mfccs = CalculateMFCCs(track.GetPreview());

        bool[] results = _machine!.Decide(mfccs);

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