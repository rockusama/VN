using SkiaSharp;

namespace VN.Handlers;

public class Character
{
    public string Name { get; set; }
    public string Blip { get; set; }
    public string Color { get; set; }

    public SKBitmap Sprite { get; set; }
    public SKBitmap DialogueBox { get; set; }
}

public class CharacterHandler
{
    public static Dictionary<string, Character> Load(string basePath)
    {
        var dict = new Dictionary<string, Character>(StringComparer.OrdinalIgnoreCase);

        foreach (var dir in Directory.GetDirectories(basePath))
        {
            var name = Path.GetFileName(dir);

            var character = new Character
            {
                Name = name,
                Color = "#" + File.ReadAllText(Path.Combine(dir, "color.txt")).Trim(),
                Blip = Path.Combine(dir, "blip.wav"),
                Sprite = SKBitmap.Decode(Path.Combine(dir, "sprite.jpg")),
                DialogueBox = SKBitmap.Decode(Path.Combine(dir, "dialogue_box.png"))
            };


            dict[name] = character;
        }

        return dict;
    }

    private static bool IsImage(string Path)
    {
        return Path.EndsWith(".png") || Path.EndsWith(".jpg") || Path.EndsWith(".jpeg");
    }

    private static bool IsAudio(string Path)
    {
        return Path.EndsWith(".wav") || Path.EndsWith(".mp3") || Path.EndsWith(".ogg");
    }
}