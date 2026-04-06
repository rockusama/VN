namespace VN.Handlers;

public class TimelineHandler
{
    public static List<double> BuildTimeline(string text, float charsPerSecond)
    {
        var timeline = new List<double>();
        double currentTime = 0;

        foreach (var c in text)
        {
            timeline.Add(currentTime);

            if (char.IsWhiteSpace(c) || c == ',')
            {
                currentTime += 1.0 / charsPerSecond;
                continue;
            }

            if (".!?".Contains(c))
            {
                currentTime += 0.25;
                continue;
            }

            double mult = "aeiouаеёиоуыэюя".Contains(char.ToLowerInvariant(c))
                ? 1.2
                : 0.8;

            currentTime += (1.0 / charsPerSecond) * mult;
        }

        return timeline;
    }
}