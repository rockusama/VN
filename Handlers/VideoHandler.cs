using FFMpegCore;
using FFMpegCore.Pipes;
using SkiaSharp;

namespace VN.Handlers;



public static class VideoHandler
{
    private const int Otstup_sleva = 30;
 
    private static List<string> WrapText(string text, SKPaint paint, float maxWidth)
    {
        var lines = new List<string>();

        foreach (var rawLine in text.Split('\n'))
        {
            var words = rawLine.Split(' ');
            string currentLine = "";

            foreach (var word in words)
            {
                var testLine = string.IsNullOrEmpty(currentLine)
                    ? word
                    : currentLine + " " + word;

                float width = paint.MeasureText(testLine);

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
        using SKFont textFont = new(SKTypeface.FromFamilyName("underdog"), 32);

        var charColor = SKColor.Parse(character.Color);
        var sprite = character.Sprite;
        var bg = character.DialogueBox;

        int newWidth = (int)(sprite.Width * height / sprite.Height);
        int newHeight = height;

        var resized = sprite.Resize(
            new SKImageInfo(newWidth, newHeight),
            SKFilterQuality.High
        );

        using SKPaint textPaint = new(textFont);
        textPaint.Color = charColor;
        textPaint.TextAlign = SKTextAlign.Left;

        int totalFrames = (int)Math.Ceiling(text.Length / charsPerSecond * fps)+60; // +1 so it wont disappear right after typing

        using SKBitmap bmp = new(width, height);
        using SKCanvas canvas = new SKCanvas(bmp);

        for (int i = 0; i < totalFrames; i++)
        {
            float elapsed = i / (float)fps;

            int charsToShow = (int)(elapsed * charsPerSecond);
            charsToShow = Math.Clamp(charsToShow, 0, text.Length);

            string visibleText = text[..charsToShow];

            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(bg, new SKPoint(0, 0));
            canvas.DrawBitmap(resized, new SKPoint(Otstup_sleva, 0));

            canvas.DrawText(character.Name, resized.Width + 200, bmp.Height * 0.2f, textPaint);

            float textX = resized.Width + 100;
            float textY = bmp.Height * 0.36f;
            float maxTextWidth = width - textX - 50;
            float lineHeight = textPaint.TextSize * 1.4f;

            var wrappedLines = WrapText(visibleText, textPaint, maxTextWidth);

            for (int li = 0; li < wrappedLines.Count; li++)
            {
                canvas.DrawText(
                    wrappedLines[li],
                    textX,
                    textY + li * lineHeight,
                    textPaint
                );
            }

            yield return new SKBitmapFrame(bmp);
        }
        
    }
    
    public static void Render()
    {
        
        var baseDir = AppContext.BaseDirectory;
        var characters = CharacterHandler.Load("Characters");
        var root = Path.GetFullPath(Path.Combine(baseDir, @"..\..\.."));
        var dialogues = ScriptHandler.Parse(Path.Combine(root, "script.txt"));

        int index = 0;

        foreach (var line in dialogues)
        {
            if (!characters.TryGetValue(line.Character, out var character))
                continue;

            var frames = CreateFrames(
                1920,
                400,
                character,
                line.Text,
                30,
                20f
            );
            
            
            string audioPath = AudioHandler.GenerateBlipTrack(
                line.Text,
                character.Blip,
                20f
            );

            var output = $"output_{index}.webm";

            var videoSource = new RawVideoPipeSource(frames) { FrameRate = 30 };

            Console.WriteLine($"[RENDER] Rendering {output}...");

            FFMpegArguments
                .FromPipeInput(videoSource)
                .AddFileInput(audioPath)
                .OutputToFile(output, true, options => options
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
        return pipe.WriteAsync(Source.Bytes, 0, Source.Bytes.Length, token);
    }
}