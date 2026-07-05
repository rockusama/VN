using VN.Handlers;

public class Program {
	public static readonly bool DebugLog = false;

	private static int Main( string[] args ) {
		Config.Parse( "config.ini" );
		// ScriptHandler.CheckForErrors( ScriptHandler.Parse( PathHelper.FromRoot( "script.txt" ) ) );
		Video.Render();

		return 0;
	}

	public static class Helper {
		public static string Root => Path.GetFullPath( Path.Combine( AppContext.BaseDirectory , @"..\..\.." ) );

		public static string FromRoot( string relative ) => Path.Combine( Root , relative );

		public static void Log( string text ) {
			if ( DebugLog )
				Console.WriteLine( text );
		}
	}
}