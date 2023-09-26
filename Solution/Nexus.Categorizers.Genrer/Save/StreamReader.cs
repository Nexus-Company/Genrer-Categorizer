using Nexus.Categorizers.Genrer.Models;
using System.Text;

namespace Nexus.Categorizers.Genrer.Save;

internal class StreamReader : IDisposable
{
    private readonly Stream input;
    public StreamReader(Stream stream)
    {
        input = stream;
    }

    public GenreConvert GetGenres()
    {
        byte[] buffer = new byte[sizeof(short)];
        Dictionary<string, short> genres = new();

        while (input.Read(buffer) > 1)
        {
            short value = BitConverter.ToInt16(buffer);

            buffer = new byte[sizeof(int)];
            input.Read(buffer);

            buffer = new byte[BitConverter.ToInt32(buffer)];
            input.Read(buffer);
            string genre = Encoding.UTF8.GetString(buffer);

            genres.Add(genre, value);
            buffer = new byte[sizeof(short)];
        }

        return new GenreConvert(genres);
    }

    public void Dispose()
    {
        input.Dispose();
    }
}