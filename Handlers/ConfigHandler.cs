using System.Globalization;
using System.Text.RegularExpressions;

public static class Config
{
    private static readonly Dictionary<string, string> _config =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly Regex _regex = new(
        @"^(?!\[)(?<key>[^=\r\n]+)=(?<value>.*)$",
        RegexOptions.Compiled
    );

    public static void Parse(string path)
    {
        _config.Clear();

        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var match = _regex.Match(line);
            if (!match.Success)
                continue;

            var key = match.Groups["key"].Value.Trim();
            var value = match.Groups["value"].Value.Trim();

            _config[key] = value;
        }
    }

    public static T? Read<T>(string key)
    {
        if (!_config.TryGetValue(key, out var value))
            return default;

        try
        {
            var type = typeof(T);

            if (type == typeof(float))
                return (T)(object)float.Parse(value, CultureInfo.InvariantCulture);

            if (type == typeof(double))
                return (T)(object)double.Parse(value, CultureInfo.InvariantCulture);

            if (type == typeof(int))
                return (T)(object)int.Parse(value, CultureInfo.InvariantCulture);

            if (type == typeof(bool))
                return (T)(object)bool.Parse(value);

            return (T)Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }
        catch
        {
            return default;
        }
    }

    public static T Read<T>(string key, T fallback)
    {
        if (!_config.TryGetValue(key, out var value))
            return fallback;

        return (T)Convert.ChangeType(value, typeof(T));
    }
}