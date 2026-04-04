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
        int count,
        int width,
        int height,
        Character character,
        List<DialogueLine> lines,
        double fps)
    {
        using SKFont textFont = new(SKTypeface.FromFamilyName("underdog"), 32);

        var charColor = SKColor.Parse(character.Color);
        var sprite = SKBitmap.FromImage(SKImage.FromEncodedData(character.SpritePath));
        var bg = SKBitmap.FromImage(SKImage.FromEncodedData(character.DgBoxPath));
        
        int newWidth = (int)(sprite.Width * height / sprite.Height);
        int newHeight = (int)(sprite.Height * height / sprite.Height);
        
        var resized = sprite.Resize(
            new SKImageInfo(newWidth, newHeight),
            SKFilterQuality.High
        );

        using SKPaint textPaint = new(textFont);
        textPaint.Color = charColor;
        textPaint.TextAlign = SKTextAlign.Left;

        var lineIndex = 0;
        
        string currentText = "";
        int startFrame = 0;
        float charsPerSecond = 20f;
        
        using SKBitmap bmp = new(width, height);
        using SKCanvas canvas = new SKCanvas(bmp);
        for (var i = 0; i < count; i++)
        {
            var currentTime = i / fps;

            while (lineIndex + 1 < lines.Count &&
                   lines[lineIndex + 1].Timecode.TotalSeconds <= currentTime)
                lineIndex++;

            var text = lineIndex < lines.Count
                ? lines[lineIndex].Text
                : string.Empty;

            if (text != currentText)
            {
                currentText = text;
                startFrame = i;
            }

            int localFrame = i - startFrame;
            float elapsed = localFrame / (float)fps;

            int charsToShow = (int)(elapsed * charsPerSecond);
            charsToShow = Math.Clamp(charsToShow, 0, currentText.Length);

            string visibleText = currentText[..charsToShow];

            canvas.Clear(SKColors.Transparent);
            canvas.DrawBitmap(bg, new SKPoint(0, 0));
            canvas.DrawBitmap(resized, new SKPoint(Otstup_sleva, 0));
            
            canvas.DrawText(character.Name, resized.Width+200, bmp.Height * 0.2f, textPaint);
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
        var character = characters["Мариса Кирисаме"];

        var root = Path.GetFullPath(Path.Combine(baseDir, @"..\..\.."));
        var dialogues = ScriptHandler.Parse(Path.Combine(root, "script.txt"));

        var frames = CreateFrames(
            300,
            1920,
            400,
            character,
            dialogues,
            30
        );

        RawVideoPipeSource videoFramesSource = new(frames) { FrameRate = 30 };
        try
        {
            Console.WriteLine("[RENDER] Rendering...");
            var success = FFMpegArguments
                .FromPipeInput(videoFramesSource)
                .OutputToFile("output.webm", true, options => options.WithVideoCodec("libvpx-vp9"))
                .ProcessSynchronously();
            Console.WriteLine("[RENDER] Rendered.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"[RENDER]{e.Message}");
            throw;
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