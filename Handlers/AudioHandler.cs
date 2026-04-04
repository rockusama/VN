using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VN.Handlers;

public class AudioHandler
{
    public static string GenerateBlipTrack(
    string text,
    string blipPath,
    float charsPerSecond)
{
    int sampleRate = 44100;
    var rand = new Random();

    // load base blip
    using var reader = new AudioFileReader(blipPath);
    var blipProvider = reader.ToSampleProvider();

    List<float> output = new();

    double currentTime = 0;

    foreach (char c in text)
    {
        // skip spaces
        if (char.IsWhiteSpace(c))
        {
            currentTime += 1.0 / charsPerSecond;
            continue;
        }

        // extra pause for punctuation
        if (c == '.' || c == ',' || c == '!' || c == '?')
        {
            currentTime += 0.15;
            continue;
        }

        // random pitch factor
        float pitch = 0.85f + (float)rand.NextDouble() * 0.3f;

        // reload reader per blip (important)
        using var r = new AudioFileReader(blipPath);
        var provider = r.ToSampleProvider();

        // apply pitch via speed change
        var pitched = new WdlResamplingSampleProvider(provider, (int)(sampleRate * pitch));

        int startSample = (int)(currentTime * sampleRate);

        // ensure buffer size
        while (output.Count < startSample)
            output.Add(0f);

        float[] buffer = new float[1024];
        int read;

        int writeIndex = startSample;

        while ((read = pitched.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < read; i++)
            {
                if (writeIndex >= output.Count)
                    output.Add(buffer[i]);
                else
                    output[writeIndex] += buffer[i];

                writeIndex++;
            }
        }

        currentTime += 1.0 / charsPerSecond;
    }

    var outPath = Path.GetTempFileName() + ".wav";

    using var writer = new WaveFileWriter(outPath, new WaveFormat(sampleRate, 1));

    foreach (var sample in output)
        writer.WriteSample(Math.Clamp(sample, -1f, 1f));

    return outPath;
}
    
    
}