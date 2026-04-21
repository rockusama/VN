using VN.Handlers;

public class Program {
	private static int Main( string[] args ) {
		Config.Parse( "config.ini" );
		ScriptHandler.CheckForErrors( ScriptHandler.Parse( PathHelper.FromRoot( "script.txt" ) ) );
		VideoHandler.Render();

		return 0;
	}

	public static class PathHelper {
		public static string Root => Path.GetFullPath( Path.Combine( AppContext.BaseDirectory , @"..\..\.." ) );

		public static string FromRoot( string relative ) => Path.Combine( Root , relative );
	}
}