using System.Text.RegularExpressions;

namespace VN.Handlers;

public class DialogueLine
{
    public string Text { get; set; }
    public string Character { get; set; }
}

public class ScriptHandler
{
    private static readonly Regex regex = new(
        @"^(?<char>[^-:]+)\s*[-:]\s*(?<text>.*)",
        RegexOptions.Compiled
    );

    public static List<DialogueLine> Parse(string path)
    {
        var result = new List<DialogueLine>();

        foreach (var raw in File.ReadAllLines(path))
        {
            if (string.IsNullOrWhiteSpace(raw))
                continue;

            var match = regex.Match(raw);
            if (!match.Success)
                continue;

            result.Add(new DialogueLine
            {
                Character = match.Groups["char"].Value.Trim(),
                Text = match.Groups["text"].Value.Trim()
            });
        }

        return result;
    }
}