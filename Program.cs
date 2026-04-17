using VN.Handlers;

public class Program
{
    private static int Main(string[] args)
    {
        Config.Parse("config.ini");
        // args - --script 
        VideoHandler.Render();
        return 0;
    }

    public static class PathHelper
    {
        public static string Root =>
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\.."));

        public static string FromRoot(string relative)
        {
            return Path.Combine(Root, relative);
        }
    }
}