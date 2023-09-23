namespace Nexus.Party.Master.Categorizer.Models;

internal struct Trainning
{
    public Guid Id { get; set; }
    public short[] Genres { get; set; }
    public double[] Mfccs { get; set; }
    public string? TrackId { get; set; } = null;
    public Trainning(short[] genres, double[] mfccs)
    {
        Genres = genres;
        Mfccs = mfccs;
        Id = Guid.NewGuid();
    }

    public Trainning(Guid id, short[] genres, double[] mfccs)
    {
        Genres = genres;
        Mfccs = mfccs;
        Id = id;
    }
}