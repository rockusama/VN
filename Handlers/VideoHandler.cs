using FFMpegCore;
using FFMpegCore.Pipes;
using SkiaSharp;

namespace VN.Handlers;

public static class VideoHandler
{
    private const int Otstup_sleva = 0;
    private const int Text_size = 36;

    private static List<string> WrapText(string text, SKPaint paint, float maxWidth)
    {
        var lines = new List<string>();

        foreach (var rawLine in text.Split('\n'))
        {
            var words = rawLine.Split(' ');
            var currentLine = "";

            foreach (var word in words)
            {
                var testLine = string.IsNullOrEmpty(currentLine)
                    ? word
                    : currentLine + " " + word;

                var width = paint.MeasureText(testLine);

                if (width <= maxWidth)
                {
                    currentLine = testLine;
                }
                else
                {
                    if (!string.IsNullOrEmpty(currentLine))
                        lines.Add(currentLine);

                    currentLine = word;
                }
            }

            if (!string.IsNullOrEmpty(currentLine))
                lines.Add(currentLine);
        }

        return lines;
    }

    private static IEnumerable<IVideoFrame> CreateFrames(
        int width,
        int height,
        Character character,
        string text,
        double fps,
        float charsPerSecond)
    {
        using SKFont textFont = new(SKTypeface.FromFamilyName("underdog"), Text_size);
        using SKFont charFont = new(SKTypeface.FromFamilyName("underdog"), 72);

        using SKPaint textPaint = new(textFont)
        {
            Color = SKColor.Parse(character.Color),
            TextAlign = SKTextAlign.Left
        };

        using SKPaint charPaint = new(charFont)
        {
            Color = SKColor.Parse(character.Color),
            TextAlign = SKTextAlign.Center
        };

        var sprite = character.Sprite;
        var bg = character.DialogueBox;

        var resized = sprite.Resize(
            new SKImageInfo(sprite.Width * height / sprite.Height, height),
            SKFilterQuality.High
        );

        var timeline = TimelineHandler.BuildTimeline(text, charsPerSecond);
        var totalDuration = timeline.Count > 0 ? timeline[^1] + 0.5 : 1;
        var totalFrames = (int)(totalDuration * fps);

        for (var i = 0; i < totalFrames; i++)
        {
            using var bmp = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bmp);

            var elapsed = i / (float)fps;

            var charsToShow = 0;
            for (var j = 0; j < timeline.Count; j++)
                if (timeline[j] <= elapsed)
                    charsToShow++;
                else
                    break;

            var visibleText = text[..Math.Min(charsToShow, text.Length)];

            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(bg, new SKPoint(0, 0));
            canvas.DrawBitmap(resized, new SKPoint(Otstup_sleva, 0));

            canvas.DrawText(character.Name, resized.Width + 400, bmp.Height * 0.2f, charPaint);

            float textX = resized.Width + 100;
            var textY = bmp.Height * 0.36f;
            var maxWidth = width - textX - 50;
            var lineHeight = textPaint.TextSize * 1.4f;

            var lines = WrapText(visibleText, textPaint, maxWidth);

            for (var li = 0; li < lines.Count; li++)
                canvas.DrawText(lines[li], textX, textY + li * lineHeight, textPaint);

            yield return new SKBitmapFrame(bmp.Copy());
        }
    }

    public static void Render()
    {
        var baseDir = AppContext.BaseDirectory;
        var characters = CharacterHandler.Load("Characters");
        var root = Path.GetFullPath(Path.Combine(baseDir, @"..\..\.."));
        var dialogues = ScriptHandler.Parse(Path.Combine(root, "script.txt"));

        var index = 0;

        foreach (var line in dialogues)
        {
            if (!characters.TryGetValue(line.Character, out var character))
                continue;

            var timeline = TimelineHandler.BuildTimeline(line.Text, 20f);

            var frames = CreateFrames(1920, 400, character, line.Text, 30, 20f);

            var audioPath = AudioHandler.GenerateBlipTrack(
                line.Text,
                character.Blip,
                timeline
            );

            var output = $"output_{index}.webm";

            var videoSource = new RawVideoPipeSource(frames) { FrameRate = 30 };

            Console.WriteLine($"[RENDER] {output}");

            FFMpegArguments
                .FromPipeInput(videoSource)
                .AddFileInput(audioPath)
                .OutputToFile(output, true, opt => opt
                    .WithVideoCodec("libvpx-vp9")
                    .WithAudioCodec("libopus")
                    .ForceFormat("webm"))
                .ProcessSynchronously();

            index++;
        }
    }
}

internal class SKBitmapFrame : IVideoFrame, IDisposable
{
    private readonly SKBitmap Source;

    public SKBitmapFrame(SKBitmap bmp)
    {
        if (bmp.ColorType != SKColorType.Bgra8888)
            throw new NotImplementedException("only 'bgra' color type is supported");
        Source = bmp;
    }

    public void Dispose()
    {
        Source.Dispose();
    }

    public int Width => Source.Width;
    public int Height => Source.Height;
    public string Format => "bgra";

    public void Serialize(Stream pipe)
    {
        pipe.Write(Source.Bytes, 0, Source.Bytes.Length);
    }

    public Task SerializeAsync(Stream pipe, CancellationToken token)
    {
        try
        {
            return pipe.WriteAsync(Source.Bytes, 0, Source.Bytes.Length, token);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
}