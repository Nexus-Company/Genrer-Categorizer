using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Nexus.Party.Master.Categorizer.Analizer;
using Nexus.Spotify.Client;
using Nexus.Spotify.Client.Models;
using System.Net.Http.Json;

public class Program
{
    public static async Task Main()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "appsettings.json"))
            .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "appsettings.Development.json"), true)
            .Build();

        using SpotifyClient client = await Utils.GetConsoleClientAsync(config);

        string json = await File.ReadAllTextAsync(Path.Combine(Environment.CurrentDirectory, "teste.json"));

        var load = JsonConvert.DeserializeObject<LoadData[]>(json);

        using MusicTrainner analizer = new();
        foreach (var batch in MusicTrainner.SplitListIntoBatches(load, 250))
        {
            List<Task> tasks = new();
            foreach (var item in batch)
            {
                var task = client.GetTrackAsync(item.Id);
                tasks.Add(task.ContinueWith((task, obj) =>
                {
                    if (task.Result.Restrictions != null)
                        return;

                    analizer.AddToTrainning(task.Result, item.Genres);
                }, null));
            }

            await Task.WhenAll(tasks.ToArray());
            await Task.Delay(1000);
        }

        await analizer.ProccessAsync();
        analizer.Trainnig();

        string outputFile = Path.Combine(Environment.CurrentDirectory, @".\Resources\Output.mma");

        await analizer.SaveToFileAsync(outputFile);

        var machineAnalizer = MusicAnalizer.ReadFile(outputFile);

        Console.Clear();
        Console.Write("Escreva o Json de músicas para testar: ");
        json = Console.ReadLine()!;
        load = JsonConvert.DeserializeObject<LoadData[]>(json);

        foreach (var item in load)
        {
            var track = await client.GetTrackAsync(item.Id);
            var rst = await machineAnalizer.GetGenreAsync(track);
        }
    }
}

public class LoadData
{
    public string Id { get; set; }
    public string[] Genres { get; set; }
}