using Accord.MachineLearning.VectorMachines;
using Accord.Math;
using Accord.Statistics.Kernels;
using Nexus.Categorizers.Genrer.Models;
using Nexus.Categorizers.Genrer.Save;
using Nexus.Spotify.Client.Models;
using System.Collections.Concurrent;
using System.Data;

namespace Nexus.Categorizers.Genrer.Analizer;

public class MusicTrainner : MusicAnalizerBase
{
    private readonly ConcurrentDictionary<string, Trainning> results;
    private readonly List<Task> downloadTasks;

    MultilabelSupportVectorMachine<Gaussian>? machine;
    public MusicTrainner()
        : base(new GenreConvert())
    {
        results = new();
        downloadTasks = new();
    }
    int mfccsCount = 0;
    public void AddToTrainning(Track track, string[] genres)
    {
        async void Add(object? obj)
        {
            try
            {
                var trainning = (AddTrainning)obj!;
                bool exist = results.TryGetValue(trainning.Track.Id, out _);

                if (exist)
                {
                    var track = results[trainning.Track.Id];
                    var genres = ConvertGenres(trainning.Genres);

                    List<short> genreRst = new();
                    genreRst.AddRange(track.Genres);

                    foreach (short item in genres)
                    {
                        if (!genreRst.Contains(item))
                            genreRst.Add(item);
                    }

                    results[trainning.Track.Id] = new Trainning(genreRst.ToArray(), track.Mfccs);

                    Console.WriteLine($"Music id \"{trainning.Track.Id}\" rewrite genres.");
                }

                var stream = await DownloadAsync(trainning.Track);

                var mfccs = CalculateMFCCs(stream);

                if (mfccsCount == 0)
                    mfccsCount = mfccs.Length;

                if (mfccsCount != mfccs.Length)
                    throw new ArgumentException("Track Mfccs length wrong");

                results.TryAdd(trainning.Track.Id, new Trainning(ConvertGenres(trainning.Genres), mfccs));
            }
            catch (Exception ex)
            {

            }
        }

        downloadTasks.Add(new Task(Add, new AddTrainning(track, genres)));
    }

    public async Task ProccessAsync()
    {
        foreach (var tasks in SplitListIntoBatches(downloadTasks, 100))
        {
            foreach (var task in tasks)
                if (task.Status == TaskStatus.Created)
                    task.Start();

            await Task.WhenAll(tasks);
        }

        // Realize a coleta de lixo após a conclusão das tarefas
        GC.Collect();
        GC.WaitForPendingFinalizers();

        var data = new List<MusicData>();

        foreach (var track in results)
            data.Add(new(track.Value, genreConvert));

        dataset = data.ToArray();
    }

    public void Trainnig()
    {
        if (dataset is null)
            throw new ArgumentException("Os dados de entrada devem ser processados anteriormente.");

        var trainingInputs = dataset.Select(unit => unit.Mfccs).ToArray();
        var trainingOutputs = dataset.Select(unit => unit.GenreLabel).ToArray();

        // Treinamento do modelo SVM multirrótulo com o dataset de treinamento
        machine = TrainSVMModel(trainingInputs, trainingOutputs);
    }

    public async Task SaveToFileAsync(string fileName)
        => await SaveToStreamAsync(new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write));

    public async Task SaveToStreamAsync(Stream stream)
    {
        using var fileSave = new StreamSave(stream, genreConvert, results);

        await fileSave.SaveToStreamAsync();
    }

    #region Auxiliary
    private short[] ConvertGenres(string[] genres)
    {
        short[] genresShort = new short[genres.Length];
        for (int i = 0; i < genres.Length; i++)
        {
            string item = genres[i];
            short genre;

            try
            {
                genre = genreConvert.Get(item);
            }
            catch (Exception)
            {
                genre = genreConvert.Add(item);
            }

            genresShort[i] = genre;
        }
        return genresShort;
    }
    public static IEnumerable<IEnumerable<T>> SplitListIntoBatches<T>(IEnumerable<T> sourceList, int batchSize)
    {
        List<IEnumerable<T>> batches = new List<IEnumerable<T>>();

        for (int i = 0; i < sourceList.Count(); i += batchSize)
        {
            batches.Add(sourceList.Skip(i).Take(batchSize).ToArray());
        }

        return batches.ToArray();
    }
    // TODO: Criar módulo que evolui o modelo atual 
    #endregion


    private struct AddTrainning
    {
        public AddTrainning(Track track, string[] genres)
        {
            Track = track ?? throw new ArgumentNullException(nameof(track));
            Genres = genres ?? throw new ArgumentNullException(nameof(genres));
        }

        public Track Track { get; set; }
        public string[] Genres { get; set; }
    }
}
