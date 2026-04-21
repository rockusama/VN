using SkiaSharp;

namespace VN.Handlers;

public class Character {
	public string Name  { get; set; }
	public string Blip  { get; set; }
	public string Color { get; set; }

	public SKBitmap Sprite      { get; set; }
	public SKBitmap DialogueBox { get; set; }
}

public class CharacterHandler {
	public static Dictionary<string , Character> Load( string basePath ) {
		var dict = new Dictionary<string , Character>( StringComparer.OrdinalIgnoreCase );

		foreach ( var dir in Directory.GetDirectories( basePath ) ) {
			var name = Path.GetFileName( dir );

			var spritePath = Directory.GetFiles( dir )
			                          .FirstOrDefault( f => IsImage( f ) && !f.Contains( "dialogue_box" ) );

			var blipPath = Directory.GetFiles( dir )
			                        .FirstOrDefault( f => IsAudio( f ) );

			var dialoguePath = Path.Combine( dir , "dialogue_box.png" );
			var colorPath = Path.Combine( dir , "color.txt" );

			if ( spritePath == null )
				throw new Exception( $"[CHAR:{name}] No sprite found (perhaps it's not .jpg or .png or .jpeg)" );

			if ( !File.Exists( dialoguePath ) )
				throw new Exception( $"[CHAR:{name}] dialogue_box.png missing" );

			if ( !File.Exists( blipPath ) )
				throw new Exception( $"[CHAR:{name}] No audio file for blip (perhaps it's not .wav or .mp3 or .ogg)" );

			if ( !File.Exists( colorPath ) )
				throw new Exception( $"[CHAR:{name}] color.txt missing" );

			var character = new Character {
				Name = name ,
				Color = "#" + File.ReadAllText( colorPath ).Trim() ,
				Blip = blipPath ,
				Sprite = SKBitmap.Decode( spritePath ) ,
				DialogueBox = SKBitmap.Decode( dialoguePath )
			};

			dict[name] = character;
		}

		return dict;
	}

	private static bool IsImage( string path ) {
		var ext = Path.GetExtension( path ).ToLowerInvariant();
		return ext == ".png" || ext == ".jpg" || ext == ".jpeg";
	}

	private static bool IsAudio( string Path ) => Path.EndsWith( ".wav" ) || Path.EndsWith( ".mp3" ) || Path.EndsWith( ".ogg" );
}