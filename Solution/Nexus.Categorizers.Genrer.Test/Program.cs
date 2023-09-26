using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Nexus.Categorizers.Genrer.Analizer;
using Nexus.Categorizers.Genrer.Test.Models;
using Nexus.Spotify.Client;
using Nexus.Spotify.Client.Models;

namespace Nexus.Categorizers.Genrer.Test;

public class Program
{
    public static async Task Main()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "appsettings.json"))
            .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "appsettings.Development.json"), true)
            .Build();

        using SpotifyClient client = await Utils.GetConsoleClientAsync(config);

        string json;

        Console.Write("Deseja criar uma nova saida de treinamento (y/n): ");
        var key = Console.ReadKey();
        Console.Clear();
        IEnumerable<LoadData> load;
        string outputFile;

        if (key.KeyChar == 'y' || key.KeyChar == 'Y')
            outputFile = await CreateNewOutput(client);
        else
            outputFile = Path.Combine(Environment.CurrentDirectory, @"Outputs\Output.mma");

        var machineAnalizer = MusicAnalizer.Load(outputFile);

        Console.Clear();
        Console.Write("Escreva o Json de músicas para testar: ");
        json = Console.ReadLine()!;
        load = JsonConvert.DeserializeObject<LoadData[]>(json)!;

        Dictionary<Track, IEnumerable<string>> rsts = new();
        foreach (var item in load)
        {
            var track = await client.GetTrackAsync(item.Id);
            var rst = await machineAnalizer.GetGenreAsync(new LocalTrack(track));
            rsts.Add(track, rst);
        }


    }

    private static async Task<string> CreateNewOutput(SpotifyClient client)
    {
        string json = await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, "teste.json"));

        IEnumerable<LoadData> load = EqualizeGenres(JsonConvert.DeserializeObject<LoadData[]>(json)!);

        using MusicTrainner analizer = new();
        foreach (var batch in MusicTrainner.SplitListIntoBatches(load, 250))
        {
            List<Task> tasks = new();
            foreach (var item in batch)
            {
                var task = client.GetTrackAsync(item.Id);
                tasks.Add(task.ContinueWith((task, obj) =>
                {
                    if (task.Result!.Restrictions != null)
                        return;

                    analizer.AddToTrainning(new LocalTrack(task.Result), item.Genres);
                }, null));
            }

            await Task.WhenAll(tasks.ToArray());
            await Task.Delay(1000);
        }

        await analizer.ProccessAsync();
        analizer.Trainnig();

        string output = Path.Combine(Environment.CurrentDirectory, "Resources");

        if (!Directory.Exists(output))
            Directory.CreateDirectory(output);

        analizer.Save(output);

        return output;
    }
    public static IEnumerable<LoadData> EqualizeGenres(IEnumerable<LoadData> inputData)
    {
        inputData = inputData
            .GroupBy(ld => ld.Id)
            .Select(group =>
            {
                var uniqueGenres = group.SelectMany(ld => ld.Genres).Distinct().ToList();
                return new LoadData(group.Key, uniqueGenres.ToArray());
            })
            .ToArray();

        var groupedByGenres = inputData
             .SelectMany(ld => ld.Genres.Select(genre => new { Genre = genre.Trim().ToLowerInvariant(), LoadData = ld }))
             .GroupBy(item => item.Genre)
             .ToDictionary(group => group.Key, group => group.Select(item => item.LoadData).ToList());

        var minGenres = groupedByGenres.Min(group => group.Value.Count);

        return groupedByGenres.SelectMany(group => group.Value.Take(minGenres));
    }
}

public record LoadData(string Id, string[] Genres);