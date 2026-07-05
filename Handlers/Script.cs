using System.ComponentModel.Design;
using System.Text;

namespace VN.Handlers;

public class DialogueLine {
	public string Character { get; set; } = "";
	public string Text { get; set; } = "";

	// public Dictionary<string, string> Command { get; set; }  = new();
	// public List<string[]> Command { get; set; } = [];
}

public static class Script {
	public static List<DialogueLine> Read( string path ) {
		var result = new List<DialogueLine>();

		var lines = File.ReadAllLines( path );

		var i = 0;

		while ( i < lines.Length ) {
			while (
				i < lines.Length &&
				string.IsNullOrWhiteSpace( lines[i] )
			)
				i++;

			if ( i >= lines.Length )
				break;

			var dialogue = new DialogueLine();

			dialogue.Character = lines[i].Trim();
			i++;

			var textBuilder = new StringBuilder();

			while (
				i < lines.Length &&
				!string.IsNullOrWhiteSpace( lines[i] ) &&
				!lines[i].StartsWith( "\\" )
			) {
				textBuilder.AppendLine( lines[i] );
				i++;
			}

			dialogue.Text = textBuilder
				.ToString()
				.Trim();

			// while (
			// 	i < lines.Length &&
			// 	lines[i].StartsWith( "\\" )
			// ) {
			// 	
			// 	// dialogue.Command.Append<string[]>(lines[i].Split( '=' ));
			// 	// Console.WriteLine( lines[i].);
			// 	i++;
			// }

			// foreach ( var command in dialogue.Command ) Console.WriteLine( command );

			result.Add( dialogue );
		}

		return result;
	}
}