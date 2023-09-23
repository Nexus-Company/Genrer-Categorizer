using Accord.MachineLearning.VectorMachines;
using Accord.Statistics.Kernels;
using Nexus.Party.Master.Categorizer.Models;
using Nexus.Spotify.Client.Models;
using StreamReader = Nexus.Party.Master.Categorizer.Save.StreamReader;

namespace Nexus.Party.Master.Categorizer.Analizer;

public class MusicAnalizer : MusicAnalizerBase
{
    MultilabelSupportVectorMachine<Gaussian> machine;

    internal MusicAnalizer(GenreConvert genreConvert, IEnumerable<MusicData> input) : base(genreConvert)
    {
        machine = TrainSVMModel(input.Select(item => item.Mfccs).ToArray(),
            input.Select(item => item.GenreLabel).ToArray());
    }

    public static MusicAnalizer ReadFile(string fileName)
        => ReadStream(File.OpenRead(fileName));

    public static MusicAnalizer ReadStream(Stream stream)
    {
        using StreamReader sr = new(stream);

        return sr.ReadToStream();
    }

    public async Task<string[]> GetGenreAsync(Track track)
    {
        var str = await DownloadAsync(track);

        var mfccs = CalculateMFCCs(str);

        bool[] results = machine.Decide(mfccs);

        List<string> result = new();

        for (int i = 0; i < results.Length; i++)
        {
            if (results[i])
                result.Add(genreConvert[i]);
        }

        return result.ToArray();
    }
}