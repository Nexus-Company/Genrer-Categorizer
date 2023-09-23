using Nexus.Categorizers.Genrer.Models;
using System.Text;
using System;

namespace Nexus.Categorizers.Genrer.Save;

internal class StreamSave : IDisposable
{
    private readonly Stream output;
    private GenreConvert genreConvert;
    private readonly IDictionary<string, Trainning> results;
    public StreamSave(Stream stream, GenreConvert genres, IDictionary<string, Trainning> musics)
    {
        output = stream;
        genreConvert = genres;
        results = musics;
    }

    public async Task SaveToStreamAsync()
    {
        await SaveHeaderAsync();

        genreConvert.SaveToStream(output);

        foreach (var item in results)
            await SaveTrainnigAsync(item.Value);
    }

    private async Task SaveHeaderAsync()
    {
        byte[] buffer = BitConverter.GetBytes(genreConvert.Count);

        await output.WriteAsync(buffer);

        buffer = BitConverter.GetBytes(results.Count);

        await output.WriteAsync(buffer);
    }

    private async Task SaveTrainnigAsync(Trainning trainning)
    {
        await output.WriteAsync(
            trainning.Id.ToByteArray());
        
        await output.WriteAsync(
            BitConverter.GetBytes(trainning.Genres.Length));

        await output.WriteAsync(
            BitConverter.GetBytes(trainning.Mfccs.Length));

        foreach (var item in trainning.Genres)
            await output.WriteAsync(
                BitConverter.GetBytes(item));

        foreach (var item in trainning.Mfccs)
            await output.WriteAsync(
                BitConverter.GetBytes(item));
    }

    public void Dispose()
    {
        output.Dispose();

        GC.Collect();
    }
}
