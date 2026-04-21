using System.Text.RegularExpressions;

namespace VN.Handlers;

public class DialogueLine {
	public string Text      { get; set; }
	public string Character { get; set; }
}

public class ScriptHandler {
	private static readonly Regex regex = new Regex( @"^(?<char>[^-:]+)\s*[-:]\s*(?<text>.*)" , RegexOptions.Compiled );

	// todo
	public static void CheckForErrors( List<DialogueLine> dialogueLines ) {
		var index = 0;

		foreach ( var line in dialogueLines ) {
			index++;

			var noChar = string.IsNullOrWhiteSpace( line.Character );
			var noText = string.IsNullOrWhiteSpace( line.Text );

			if ( noChar && noText ) {
				Console.WriteLine( $"[SCRIPT:{index}] Missing character and text." );
				continue;
			}

			if ( noChar ) {
				Console.WriteLine( $"[SCRIPT:{index}] Missing character." );
				continue;
			}

			if ( noText ) Console.WriteLine( $"[SCRIPT:{index}] Missing text." );
		}
	}

	public static List<DialogueLine> Parse( string path ) {
		var result = new List<DialogueLine>();

		foreach ( var raw in File.ReadAllLines( path ) ) {
			if ( string.IsNullOrWhiteSpace( raw ) )
				continue;

			var match = regex.Match( raw );
			if ( !match.Success )
				continue;

			result.Add( new DialogueLine {
				Character = match.Groups["char"].Value.Trim() ,
				Text = match.Groups["text"].Value.Trim()
			} );
		}

		return result;
	}
}

public class ScriptHelper { }