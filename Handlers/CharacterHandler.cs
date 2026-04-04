using System.Drawing;
using System.IO;
using SkiaSharp;

namespace VN.Handlers;

public class Character
{
    public string Name { get; set; }
    public string DgBoxPath { get; set; }
    public string Blip {get; set;}
    public string SpritePath { get; set; } // fanta блять
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
                SpritePath = Path.Combine(dir, "sprite.jpg"),
                DgBoxPath = Path.Combine(dir, "dialogue_box.png"),
                Color = "#"+File.ReadAllText(Path.Combine(dir, "color.txt")).Trim()
            };

            character.Sprite = SKBitmap.Decode(character.SpritePath);
            character.DialogueBox = SKBitmap.Decode(character.DgBoxPath);

            dict[name] = character;
        }
        
        return dict;
    }

    public static SKColor ColorConverter(string color)
    {
        SKColor t2 = SKColor.Parse(color);
        Console.WriteLine(t2);
        return t2;
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