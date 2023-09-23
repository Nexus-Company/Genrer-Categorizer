namespace Nexus.Party.Master.Categorizer.Models;

internal class MusicData
{
    public double[] Mfccs { get; set; }
    public bool[] GenreLabel { get; set; }
    public Guid Id { get; set; }
    public MusicData(Guid id, double[] mfccs, bool[] genreLabel)
    {
        Mfccs = mfccs;
        GenreLabel = genreLabel;
        Id = id;
    }

    public MusicData(Trainning trainning, GenreConvert genreConvert)
    {
        Mfccs = trainning.Mfccs;
        GenreLabel = new bool[genreConvert.Count];
        Id = trainning.Id;

        for (int i = 0; i < GenreLabel.Length; i++)
        {
            short actual = genreConvert.ElementAt(i);

            if (trainning.Genres.Contains(actual))
            {
                GenreLabel[i] = true;
            }
            else
            {
                GenreLabel[i] = false;
            }
        }
    }
}
