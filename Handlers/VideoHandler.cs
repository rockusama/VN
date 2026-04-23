using FFMpegCore;
using FFMpegCore.Pipes;
using SkiaSharp;

namespace VN.Handlers;

public class VideoHandler {
	private static IEnumerable<IVideoFrame> CreateFrames(
		int width ,
		int height ,
		Character character ,
		string text ,
		double fps ,
		float charsPerSecond
	) {
		using var textFont = new SKFont( SKTypeface.FromFamilyName( "underdog" ) , _config.textSize );
		using var charFont = new SKFont( SKTypeface.FromFamilyName( "underdog" ) , _config.charTextSize );

		using var textPaint = new SKPaint( textFont );
		textPaint.Color = SKColor.Parse( character.Color );

		using var charPaint = new SKPaint( charFont );
		charPaint.Color = SKColor.Parse( character.Color );

		var sprite = character.Sprite;
		var bg = character.DialogueBox;
		SKBitmap resized;
		if ( _config.charSpriteResizeY == 0 && _config.charSpriteResizeX == 0 ) {
			resized = sprite.Resize(
				new SKImageInfo( sprite.Width * height / sprite.Height , height ) ,
				new SKSamplingOptions()
			);
		}
		else {
			resized = sprite.Resize(
				new SKImageInfo( _config.charSpriteResizeX , _config.charSpriteResizeY ) ,
				new SKSamplingOptions()
			);
		}


		var timeline = TimelineHandler.BuildTimeline( text , charsPerSecond );
		var totalDuration = timeline.Count > 0? timeline[^1] + 0.5:1;
		var totalFrames = (int)( totalDuration * fps );

		for ( var i = 0 ; i < totalFrames ; i++ ) {
			using var bmp = new SKBitmap( width , height );
			using var canvas = new SKCanvas( bmp );

			var elapsed = i / (float)fps;

			var charsToShow = 0;
			for ( var j = 0 ; j < timeline.Count ; j++ ) {
				if ( timeline[j] <= elapsed )
					charsToShow++;
				else
					break;
			}

			var visibleText = text[..Math.Min( charsToShow , text.Length )];

			canvas.Clear( SKColors.Transparent );
			canvas.DrawBitmap( bg , new SKPoint( 0 , 0 ) );
			canvas.DrawBitmap( resized , new SKPoint( _config.charSpritePosX , _config.charSpritePosY ) );

			canvas.DrawText( character.Name , _config.textPadding == "no"? _config.charTextX:resized.Width + _config.textX , _config.charTextY , _config.charAlignment , charFont , charPaint );

			float textX = _config.textPadding == "no"? _config.textX:resized.Width + _config.textX;
			var textY = _config.textY;
			var maxTextWidth = width - textX - 50;
			var lineHeight = textFont.Size * 1.4f;

			var wrappedLines = Helper.WrapText( visibleText , textPaint , maxTextWidth );

			var globalCharIndex = 0;

			for ( var li = 0 ; li < wrappedLines.Count ; li++ ) {
				var x = textX;
				var y = textY + li * lineHeight;

				foreach ( var c in wrappedLines[li] ) {
					var charTime = globalCharIndex < timeline.Count
						? timeline[globalCharIndex]
						:0f;

					var t = elapsed - charTime;
					if ( t < 0 ) t = 0;

					var offsetY = Helper.Bounce( (float)t );

					// Console.WriteLine( ( y - offsetY ).ToString("0.000000") + " " + c); //
					canvas.DrawText(
						c.ToString() , x , !AudioHandler._config.IsUnpronounceable( c )? y - offsetY:y , _config.textAlignment , textFont , textPaint
					);

					x += textFont.MeasureText( c.ToString() );
					globalCharIndex++;
				}
			}

			yield return new SKBitmapFrame( bmp.Copy() );
		}

		Console.Write( "[VIDEO DONE]" );
	}


	public static void Render() {
		var characters = CharacterHandler.Load( "Characters" );
		var dialogues = ScriptHandler.Parse( Program.PathHelper.FromRoot( "script.txt" ) );
		var index = 1;

		foreach ( var line in dialogues ) {
			if ( !characters.TryGetValue( line.Character , out var character ) )
				continue;

			var timeline = TimelineHandler.BuildTimeline( line.Text , _config._charsPerSecond );
			var frames = CreateFrames( _config.videoWidth , _config.videoHeight , character , line.Text , 30 , _config._charsPerSecond );
			var audioPath = AudioHandler.GenerateBlipTrack( line.Text , character.Blip , timeline );
			var videoSource = new RawVideoPipeSource( frames ) { FrameRate = 30 };

			var output = $"{index}.webm";
			FFMpegArguments
				.FromPipeInput( videoSource )
				.AddFileInput( audioPath )
				.OutputToFile( output , true , opt => opt
				                                      .WithVideoCodec( "libvpx-vp9" )
				                                      .WithAudioCodec( "libopus" )
				                                      .ForceFormat( "webm" ) )
				.ProcessSynchronously();
			Console.WriteLine( $"[RENDER] {output}" );
			index++;
		}

		Console.WriteLine( $"Done rendering {index} files." );
	}

	private class _config {
		public static readonly int   videoWidth      = Config.Read<int>( "width" );
		public static readonly int   videoHeight     = Config.Read<int>( "height" );
		public static readonly float _charsPerSecond = Config.Read<float>( "characters_per_second" );

		public static readonly int         textY         = Config.Read<int>( "text_y" );
		public static readonly SKTextAlign textAlignment = Config.Read<SKTextAlign>( "text_alignment" );
		public static readonly int         textSize      = Config.Read<int>( "text_size" );
		public static readonly int         textX         = Config.Read<int>( "text_x" );
		public static readonly string      textPadding   = Config.Read<string>( "text_padding_from_image" , "no" );

		public static readonly int         charTextY         = Config.Read<int>( "char_text_y" );
		public static readonly int         charTextX         = Config.Read<int>( "char_text_x" );
		public static readonly int         charTextSize      = Config.Read<int>( "char_text_size" );
		public static readonly SKTextAlign charAlignment     = Config.Read<SKTextAlign>( "char_text_alignment" );
		public static readonly int         charSpritePosY    = Config.Read<int>( "char_sprite_y" );
		public static readonly int         charSpritePosX    = Config.Read<int>( "char_sprite_x" );
		public static readonly int         charSpriteResizeY = Config.Read<int>( "char_sprite_resize_y" );
		public static readonly int         charSpriteResizeX = Config.Read<int>( "char_sprite_resize_x" );
	}

	private static class Helper {
		public static List<string> WrapText( string text , SKPaint paint , float maxWidth ) {
			var lines = new List<string>();

			foreach ( var rawLine in text.Split( '\n' ) ) {
				var words = rawLine.Split( ' ' );
				var currentLine = "";

				foreach ( var word in words ) {
					var testLine = string.IsNullOrEmpty( currentLine )
						? word
						:currentLine + " " + word;

					var linewidth = paint.MeasureText( testLine );

					if ( linewidth <= maxWidth )
						currentLine = testLine;
					else {
						if ( !string.IsNullOrEmpty( currentLine ) )
							lines.Add( currentLine );

						currentLine = word;
					}
				}

				if ( !string.IsNullOrEmpty( currentLine ) )
					lines.Add( currentLine );
			}

			return lines;
		}

		public static float Bounce( float t ) {
			var amplitude = Config.Read<float>( "amplitude" );
			var frequency = Config.Read<float>( "frequency" );
			var decay = Config.Read<float>( "decay" );

			var returned = (float)( Math.Sin( t * frequency ) * amplitude * Math.Exp( -t * decay ) );
			return returned;
		}
	}
}

class SKBitmapFrame : IVideoFrame , IDisposable {
	private readonly SKBitmap Source;

	public SKBitmapFrame( SKBitmap bmp ) {
		if ( bmp.ColorType != SKColorType.Bgra8888 )
			throw new NotImplementedException( "only 'bgra' color type is supported" );
		Source = bmp;
	}

	public void Dispose() { Source.Dispose(); }

	public int    Width  => Source.Width;
	public int    Height => Source.Height;
	public string Format => "bgra";

	public void Serialize( Stream pipe ) { pipe.Write( Source.Bytes , 0 , Source.Bytes.Length ); }

	public Task SerializeAsync( Stream pipe , CancellationToken token ) {
		try { return pipe.WriteAsync( Source.Bytes , 0 , Source.Bytes.Length , token ); }
		catch ( Exception e ) {
			Console.WriteLine( e );
			throw;
		}
	}
}