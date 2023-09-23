using System.Text;

namespace Nexus.Categorizers.Genrer.Models;

internal struct GenreConvert
{
    private readonly IDictionary<string, short> keys;
    public readonly string this[int position]
    {
        get => keys.ElementAt(position).Key;
    }
    public readonly int Count
        => keys.Count;

    public GenreConvert() : this(new Dictionary<string, short>())
    {
    }

    public GenreConvert(IDictionary<string, short> keys)
    {
        this.keys = new Dictionary<string, short>(keys.OrderBy(x => x.Value));
    }

    public readonly short Add(string genre)
    {
        lock (keys)
        {
            var last = keys.LastOrDefault();

            if (last.Equals(default(KeyValuePair<string, short>)))
                last = new(string.Empty, -1);

            short value = (short)(last.Value + 1);

            keys.TryAdd(genre.ToLowerInvariant(), value);

            return value;
        }
    }

    public readonly short Get(string genre)
        => keys[genre.ToLowerInvariant()];
    public readonly short ElementAt(int position)
        => keys.ElementAt(position).Value;

    public readonly void SaveToStream(Stream output)
    {
        byte[] buffer;
        foreach (var item in keys)
        {
            buffer = Encoding.ASCII.GetBytes(item.Key);

            output.Write(BitConverter.GetBytes(item.Value));
            output.Write(BitConverter.GetBytes(buffer.Length));
            output.Write(buffer);
        }
    }
}