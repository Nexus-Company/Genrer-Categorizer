using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math;
using Accord.Statistics.Kernels;
using NAudio.Wave;
using Nexus.Party.Master.Categorizer.Models;
using Nexus.Spotify.Client.Models;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Options;

namespace Nexus.Party.Master.Categorizer.Analizer;

public abstract class MusicAnalizerBase : IDisposable
{
    public Guid Id { get; private set; }
    private readonly string TempPath;
    private protected readonly GenreConvert genreConvert;
    private protected IEnumerable<MusicData>? dataset;
    internal MusicAnalizerBase(GenreConvert genreConvert)
    {
        Id = Guid.NewGuid();
        TempPath ??= Path.Combine(Path.GetTempPath(), "Music-Analizer");
        this.genreConvert = genreConvert;

        if (!Directory.Exists(TempPath))
            Directory.CreateDirectory(TempPath);

        TempPath = Path.Combine(TempPath, "Previews");

        if (!Directory.Exists(TempPath))
            Directory.CreateDirectory(TempPath);
    }

    #region Auxiliary
    private protected async Task<Stream> DownloadAsync(Track track)
    {
        string tempFile = Path.Combine(TempPath, $"{track.Id}.mp3");

        bool fileExists = File.Exists(tempFile);

        var stream = new FileStream(tempFile, FileMode.OpenOrCreate, FileAccess.ReadWrite);

        if (!fileExists)
            await track.DownloadPreviewAsync(stream);

        return stream;
    }

    private protected static double[] CalculateMFCCs(Stream stream)
    {
        var samplesList = new List<float>();

        using (stream)
        {
            var mp3Reader = new Mp3FileReader(stream);

            var pmc = WaveFormatConversionStream.CreatePcmStream(mp3Reader);
            var buffer = new float[1024];

            var provider = pmc.ToSampleProvider();

            while (provider.Read(buffer, 0, buffer.Length) > 0)
                samplesList.AddRange(buffer);
        }

        // Configurar o extrator de MFCCs
        var mfcc = new MfccExtractor(new MfccOptions
        {
            SamplingRate = 44100,
            FeatureCount = 13,
            FrameDuration = 0.0256,
            HopDuration = 0.010,
            FilterBankSize = 26,
            LowFrequency = 20,
            HighFrequency = 20000
        });

        float[] samples = samplesList.ToArray();

        var mfccs = mfcc.ComputeFrom(samples, 0, samples.Length);

        return ConvertMfccsToDouble(mfccs.ToArray());
    }

    private protected MultilabelSupportVectorMachine<Gaussian> TrainSVMModel(double[][] inputs, bool[][] outputs)
    {
        // Create a new Linear kernel
        // Create the Multi-class learning algorithm for the machine
        var teacher = new MultilabelSupportVectorLearning<Gaussian>()
        {
            // Configure the learning algorithm to use SMO to train the
            //  underlying SVMs in each of the binary class subproblems.
            Learner = (p) => new SequentialMinimalOptimization<Gaussian>()
        };

        // Run the learning algorithm
        var machine = teacher.Learn(inputs, outputs);

        return machine;
    }
    #endregion

    #region Locals
    // Método para converter os coeficientes MFCCs para double[]
    private static double[] ConvertMfccsToDouble(float[][] mfccs)
    {
        // Aqui você pode converter os coeficientes MFCCs de float[][] para double[]
        double[][] convertedMfccs = new double[mfccs.Length][];

        for (int i = 0; i < mfccs.Length; i++)
        {
            float[] floats = mfccs[i];
            double[] doubles = new double[floats.Length];

            for (int x = 0; x < floats.Length; x++)
            {
                doubles[x] = floats[x];
            }

            convertedMfccs[i] = NormalizeData(doubles);
        }

        return Matrix.Concatenate(convertedMfccs);
    }

    static double[] NormalizeData(double[] data)
    {
        double maxPositive = double.MinValue;
        double minNegative = double.MaxValue;

        // Encontra o máximo valor positivo e o mínimo valor negativo
        foreach (var value in data)
        {
            if (value > 0 && value > maxPositive)
            {
                maxPositive = value;
            }
            else if (value < 0 && value < minNegative)
            {
                minNegative = value;
            }
        }

        // Calcula a amplitude dos valores para normalizar
        double amplitude = Math.Max(maxPositive, -minNegative);

        // Normaliza os dados
        double[] normalizedData = new double[data.Length];
        for (int i = 0; i < data.Length; i++)
        {
            if (data[i] > 0)
            {
                normalizedData[i] = data[i] / amplitude;
            }
            else
            {
                normalizedData[i] = data[i] / amplitude;
            }
        }

        return normalizedData;
    }
    #endregion

    public void Dispose()
    {
        if (Directory.Exists(TempPath))
            Directory.Delete(TempPath, true);
    }
}