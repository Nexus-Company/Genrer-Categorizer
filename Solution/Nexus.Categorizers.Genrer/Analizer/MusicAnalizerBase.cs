using Accord.IO;
using Accord.MachineLearning.VectorMachines;
using Accord.MachineLearning.VectorMachines.Learning;
using Accord.Math;
using Accord.Statistics.Kernels;
using NAudio.Wave;
using Nexus.Categorizers.Genrer.Models;
using NWaves.FeatureExtractors;
using NWaves.FeatureExtractors.Options;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace Nexus.Categorizers.Genrer.Analizer;
#pragma warning disable SYSLIB0011
public abstract class MusicAnalizerBase : IDisposable
{
    private const string WeigthsFile = "Weigths.mma";
    private const string GenresFile = "Genres.gens";
    private readonly string TempPath;
    private protected readonly GenreConvert genreConvert;
    private protected IEnumerable<MusicData>? dataset;
    private protected MultilabelSupportVectorMachine<Gaussian>? _machine;

    public Guid Id { get; private set; }

    internal MusicAnalizerBase(GenreConvert genreConvert)
        : this(genreConvert, machine: null)
    {

    }

    private protected MusicAnalizerBase(GenreConvert genreConvert, MultilabelSupportVectorMachine<Gaussian>? machine)
    {
        Id = Guid.NewGuid();
        this.genreConvert = genreConvert;
        _machine = machine;

        TempPath = Path.Combine(Path.GetTempPath(), "Music-Analizer");

        if (!Directory.Exists(TempPath))
            Directory.CreateDirectory(TempPath);
    }

    private protected MusicAnalizerBase(GenreConvert genres, Stream str)
        : this(genres)
    {
        try
        {
            BinaryFormatter formatter = new();
            _machine = (MultilabelSupportVectorMachine<Gaussian>)formatter.Deserialize(str);
        }
        catch (Exception ex)
        {
            throw ex;
        }
    }

    public void Save(string file)
    {
        string fileName = Path.Combine(TempPath, "Saves");

        if (!Directory.Exists(fileName))
            Directory.CreateDirectory(fileName);

        using ZipArchive zip = ZipFile.Open(file, ZipArchiveMode.Create);

        fileName = Path.Combine(fileName, $@"{DateTime.Now:dd_MM_yyyy_ss}.mma");

        Serializer.Save(_machine, fileName);

        zip.CreateEntryFromFile(fileName, WeigthsFile, CompressionLevel.SmallestSize);

        fileName = $"{fileName}.gens";

        using MemoryStream fileStr = new();
        genreConvert.SaveToStream(fileStr);
        fileStr.Position = 0;

        // Create and copy MemoryStream
        ZipArchiveEntry entry = zip.CreateEntry(GenresFile);
        using Stream entryStream = entry.Open();
        fileStr.CopyTo(entryStream);
    }

    public static MusicAnalizerBase Load(string file)
    {
        throw new NotImplementedException();
    }

    private protected static Stream GetLoader(string file, out GenreConvert genres)
    {
        using ZipArchive zip = ZipFile.Open(file, ZipArchiveMode.Read);

        var entry = zip.GetEntry(GenresFile);
        var str = new Save.StreamReader(entry!.Open());
        genres = str.GetGenres();

        return zip.GetEntry(WeigthsFile)!.Open();
    }

    #region Auxiliary
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

    private protected static MultilabelSupportVectorMachine<Gaussian> TrainSVMModel(double[][] inputs, bool[][] outputs)
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
        {
        }
    }
}