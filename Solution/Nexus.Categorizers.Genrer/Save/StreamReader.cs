using Nexus.Categorizers.Genrer.Analizer;
using Nexus.Categorizers.Genrer.Models;
using System.Linq;
using System.Text;

namespace Nexus.Categorizers.Genrer.Save;

internal class StreamReader : IDisposable
{
    private readonly Stream input;
    public StreamReader(Stream stream)
    {
        input = stream;
    }

    public MusicAnalizer ReadToStream()
    {
        var genres = ReadHeader(out int results);

        var inputs = ReadResults(results, genres.Count);

        return new MusicAnalizer(genres, inputs);
    }

    private GenreConvert ReadHeader(out int results)
    {
        int genresCount;
        byte[] buffer = new byte[sizeof(int) * 2];

        input.Read(buffer);

        genresCount = BitConverter.ToInt32(buffer, 0);
        results = BitConverter.ToInt32(buffer, sizeof(int));

        Dictionary<string, short> genres = new();

        for (int i = 0; i < genresCount; i++)
        {
            buffer = new byte[sizeof(short)];
            input.Read(buffer);
            short value = BitConverter.ToInt16(buffer);

            buffer = new byte[sizeof(int)];
            input.Read(buffer);

            buffer = new byte[BitConverter.ToInt32(buffer)];
            input.Read(buffer);
            string genre = Encoding.ASCII.GetString(buffer);

            genres.Add(genre, value);
        }

        return new GenreConvert(genres);
    }

    private IEnumerable<MusicData> ReadResults(int count, int genresTotal)
    {
        List<MusicData> results = new();
        byte[] buffer;

        for (int i = 0; i < count; i++)
        {
            #region Get Music Data
            buffer = new byte[16];
            input.Read(buffer);
            Guid id = new(buffer);

            buffer = new byte[sizeof(int) * 2];

            input.Read(buffer);

            int genresCount = BitConverter.ToInt32(buffer),
                mfccsCount = BitConverter.ToInt32(buffer, sizeof(int));

            int[] genres = new int[genresCount];
            buffer = new byte[sizeof(short)];

            for (int genre = 0; genre < genresCount; genre++)
            {
                input.Read(buffer);
                genres[genre] = BitConverter.ToInt16(buffer);
            }

            buffer = new byte[sizeof(double) * mfccsCount];
            input.Read(buffer);

            double[] mfccs = new double[mfccsCount];

            for (int mfcc = 0; mfcc < mfccsCount; mfcc++)
            {
                mfccs[mfcc] = BitConverter.ToDouble(buffer, sizeof(double) * mfcc);
            }
            #endregion

            results.Add(new MusicData(id, mfccs, GetOutput(genres, genresTotal)));
        }

        return results;
    }

    private bool[] GetOutput(int[] genres, int genresTotal)
    {
        bool[] labels = new bool[genresTotal];

        for (int i = 0; i < genresTotal; i++)
        {
            if (genres.Contains(i))
                labels[i] = true;
        }

        return labels;
    }

    public void Dispose()
    {
        input.Dispose();
    }
}