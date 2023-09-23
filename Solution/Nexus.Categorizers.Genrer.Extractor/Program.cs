using Microsoft.Extensions.Configuration;
using NAudio.Wave;
using Newtonsoft.Json;
using Nexus.Spotify.Client;
using Nexus.Spotify.Client.Models;

namespace Nexus.Categorizers.Genrer.Extractor;

public class Program
{
#pragma warning disable CS8618 // O campo não anulável precisa conter um valor não nulo ao sair do construtor. Considere declará-lo como anulável.
    static SpotifyClient spotClient;
#pragma warning restore CS8618 // O campo não anulável precisa conter um valor não nulo ao sair do construtor. Considere declará-lo como anulável.
    public static async Task Main()
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "appsettings.json"))
            .AddJsonFile(Path.Combine(Environment.CurrentDirectory, "appsettings.Development.json"), true)
            .Build();

        spotClient = await Utils.GetConsoleClientAsync(config);

        List<LoadData> loads = new();

        using (spotClient)
        {
            var playlist = await GetPlaylistAsync();

            var tracks = await playlist.GetTracksAsync();

            foreach (var item in tracks)
            {
                var track = item.Track;
                var genres = await GetGenresAsync(track);

                loads.Add(new(track.Id, genres));
            }
        }

        Console.Clear();
        Console.WriteLine(JsonConvert.SerializeObject(loads.ToArray()));
        Console.ReadLine();
    }

    public async static Task<Playlist> GetPlaylistAsync()
    {
        var playlists = await spotClient.GetUserPlaylistAsync();

        for (int i = 0; i < playlists.Count(); i++)
        {
            var item = playlists.ElementAt(i);
            Console.WriteLine($"{i} - {item.Name}");
        }

        Console.Write("\nEscolha uma playlist para categorizar: ");

        _ = int.TryParse(Console.ReadLine(), out int rst);

        return playlists.ElementAt(rst);
    }

    public async static Task<string[]> GetGenresAsync(Track track)
    {
        Console.Clear();

        using var ms = new MemoryStream();

        Console.WriteLine($"{track.Name}");
        Console.Write("Defina os gêneros: ");

        try
        {
            await track.DownloadPreviewAsync(ms);

            // Criar um objeto WaveStream para ler o MemoryStream
            WaveStream waveStream = new Mp3FileReader(ms);

            // Criar um objeto WaveOut para reproduzir o som
            using var outputDevice = new WaveOutEvent();

            outputDevice.Init(waveStream);
            outputDevice.Play();

            var genres = Console.ReadLine()?.Split(",") ??
                new string[0];

            outputDevice.Stop();

            return genres;
        }
        catch (Exception ex)
        {
            return Console.ReadLine()?.Split(",") ??
                new string[0];
        }
    }
}
public class LoadData
{
    public LoadData(string id, string[] genres)
    {
        Id = id;
        Genres = genres;
    }

    public string Id { get; set; }
    public string[] Genres { get; set; }
}