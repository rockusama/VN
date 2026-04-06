using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VN.Handlers;

/// <summary>
/// KNOWN ISSUES
/// DOES NOT SKIP EMOTICONS LIKE :3
/// 
/// </summary>

public class AudioHandler
{
    private static bool IsPunctuation(char c)
    {
        return ".!?".Contains(char.ToLowerInvariant(c));
    }
    
    private static bool IsUnpronounceable(char c)
    {
        return "@#&%".Contains(char.ToLowerInvariant(c));
    }
    
    public static string GenerateBlipTrack(
        string text,
        string blipPath,
        List<double> timeline)
    {
        int sampleRate = 44100;
        var rand = new Random();

        List<float> output = new();

        for (int idx = 0; idx < text.Length; idx++)
        {
            char c = text[idx];
            double time = timeline[idx];

            if (char.IsWhiteSpace(c) 
                || c == ',' 
                || IsPunctuation(c) 
                || IsUnpronounceable(c)
                )
                continue;

            float pitch = 0.85f + (float)rand.NextDouble() * 0.3f;

            using var reader = new AudioFileReader(blipPath);
            var provider = reader.ToSampleProvider();
            var pitched = new WdlResamplingSampleProvider(provider, (int)(sampleRate * pitch));

            int startSample = (int)(time * sampleRate);

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
        }

        string path = Path.GetTempFileName() + ".wav";

        using var writer = new WaveFileWriter(path, new WaveFormat(sampleRate, 1));
        foreach (var s in output)
            writer.WriteSample(Math.Clamp(s, -1f, 1f));

        return path;
    }
}