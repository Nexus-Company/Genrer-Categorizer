using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Nexus.Categorizers.Genrer.Analizer;
using Nexus.Spotify.Client;

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
        
        if (key.KeyChar == 'y' || key.KeyChar == 'Y')
            await CreateNewOutput(client);

        string outputFile = Path.Combine(Environment.CurrentDirectory, @".\Resources\Output.mma");

        var machineAnalizer = MusicAnalizer.ReadFile(outputFile);

        Console.Clear();
        Console.Write("Escreva o Json de músicas para testar: ");
        json = Console.ReadLine()!;
        load = JsonConvert.DeserializeObject<LoadData[]>(json)!;

        foreach (var item in load)
        {
            var track = await client.GetTrackAsync(item.Id);
            var rst = await machineAnalizer.GetGenreAsync(track);
        }
    }


    private static async Task CreateNewOutput(SpotifyClient client)
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
    }
    public static LoadData[] EqualizeGenres(IEnumerable<LoadData> inputData)
    {
        // Encontre o número máximo de gêneros em um único item
        int maxGenreCount = inputData.Max(item => item.Genres.Length);

        // Crie uma lista de saída que terá todos os gêneros com a mesma quantidade
        List<LoadData> equalizedList = new();

        // Itere sobre cada item da entrada
        foreach (var item in inputData)
        {
            // Para cada item, crie cópias adicionais com os gêneros repetidos para igualar a quantidade
            foreach (var genre in item.Genres)
            {
                var newItem = new LoadData
                {
                    Id = item.Id,
                    Genres = Enumerable.Repeat(genre, maxGenreCount).ToArray()
                };
                equalizedList.Add(newItem);
            }
        }

        return equalizedList.ToArray();
    }
}

public class LoadData
{
    public string Id { get; set; }
    public string[] Genres { get; set; }
}

