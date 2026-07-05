using System.Collections.Concurrent;
using System.Diagnostics;
using FFMpegCore;
using FFMpegCore.Pipes;
using SkiaSharp;

///
/// todo move char reading from framing loop
/// todo make text color, char color, font changing for char, font changing for text

namespace VN.Handlers;

public class Video {
	private static IEnumerable<IVideoFrame> CreateFrames(
		int width ,
		int height ,
		Character character ,
		string text ,
		double fps ,
		float charsPerSecond
	) {
		var watch = Stopwatch.StartNew();

		var charSpritePosY = _config.charSpritePosY;
		var charSpritePosX = _config.charSpritePosX;
		var charTextX = _config.charTextX;
		var dialogueTextX = _config.textX;
		var charTextY = _config.charTextY;
		var charAlignment = _config.charAlignment;
		var textAlignment = _config.textAlignment;

		using var charFont = new SKFont( SKTypeface.FromFamilyName( "underdog" ) , _config.charTextSize );
		using var charPaint = new SKPaint( charFont );
		charPaint.Color = SKColor.Parse( character.Color );

		var textColor = SKColor.Parse( character.Color );

		var sprite = character.Sprite;
		var bg = character.DialogueBox;

		var resizeStart = watch.Elapsed;
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

		Program.Helper.Log( "Sprite resize took " + ( watch.Elapsed - resizeStart ) );

		var timeline = Timeline.Build( text , charsPerSecond );
		var totalDuration = timeline.Count > 0? timeline[^1] + 0.5 : 1;
		var textPadding = _config.textPadding;

		var charNameX = !textPadding? charTextX : resized.Width + dialogueTextX;

		var baseFrameStart = watch.Elapsed;
		using var baseFrame = CreateBaseFrame(
			width , height , bg , resized ,
			charSpritePosX , charSpritePosY ,
			character.Name , charNameX , charTextY , charAlignment ,
			charFont , charPaint
		);
		Program.Helper.Log( "Base frame took " + ( watch.Elapsed - baseFrameStart ) );

		float textX = !textPadding? dialogueTextX : resized.Width + dialogueTextX;
		var textY = _config.textY;
		var maxTextWidth = width - textX - 50;
		var fontSize = _config.textSize;
		var lineHeight = fontSize * 1.4f;

		var frameCount = (int)( totalDuration * fps );
		var frameBitmaps = new SKBitmap[frameCount];

		Program.Helper.Log( "Frame count: " + frameCount + " , resolution: " + width + "x" + height );
		var loopStart = watch.Elapsed;

		Parallel.For(
			0 , frameCount ,
			() => {
				var localFont = new SKFont( SKTypeface.FromFamilyName( "underdog" ) , fontSize );
				localFont.Edging = SKFontEdging.Alias;
				var localPaint = new SKPaint( localFont ) { Color = textColor , IsAntialias = false };
				return ( localFont , localPaint );
			} ,
			( i , loop , local ) => {
				var (localFont , localPaint) = local;
				var elapsed = i / (float)fps;

				var charsToShow = 0;
				while ( charsToShow < timeline.Count && timeline[charsToShow] <= elapsed )
					charsToShow++;

				var visibleText = text[..Math.Min( charsToShow , text.Length )];
				var wrappedLines = Helper.WrapText( visibleText , localPaint , maxTextWidth );

				var frameBmp = baseFrame.Copy();
				using var canvas = new SKCanvas( frameBmp );

				var globalCharIndex = 0;

				for ( var li = 0 ; li < wrappedLines.Count ; li++ ) {
					var x = textX;
					var y = textY + li * lineHeight;

					foreach ( var c in wrappedLines[li] ) {
						var charTime = globalCharIndex < timeline.Count
							? timeline[globalCharIndex]
							: 0f;

						var t = elapsed - charTime;
						if ( t < 0 ) t = 0;

						var offsetY = Helper.Bounce( (float)t );

						canvas.DrawText(
							c.ToString() , x , !Audio._config.IsUnpronounceable( c )? y - offsetY : y , textAlignment , localFont , localPaint
						);

						x += localFont.MeasureText( c.ToString() );
						globalCharIndex++;
					}
				}

				frameBitmaps[i] = frameBmp;
				return local;
			} ,
			local => {
				local.Item1.Dispose();
				local.Item2.Dispose();
			}
		);

		Program.Helper.Log( "Parallel loop took " + ( watch.Elapsed - loopStart ) );

		var yieldStart = watch.Elapsed;
		for ( var i = 0 ; i < frameCount ; i++ )
			yield return new SKBitmapFrame( frameBitmaps[i] );
		Program.Helper.Log( "Yield loop took " + ( watch.Elapsed - yieldStart ) );

		watch.Stop();
		Program.Helper.Log( "Video took " + watch.Elapsed );
	}

	private static SKBitmap CreateBaseFrame(
		int width ,
		int height ,
		SKBitmap bg ,
		SKBitmap resizedSprite ,
		int spriteX ,
		int spriteY ,
		string charName ,
		float charNameX ,
		int charNameY ,
		SKTextAlign charAlignment ,
		SKFont charFont ,
		SKPaint charPaint
	) {
		var baseBmp = new SKBitmap( width , height );
		using var canvas = new SKCanvas( baseBmp );

		canvas.Clear( SKColors.Transparent );
		canvas.DrawBitmap( bg , new SKPoint( 0 , 0 ) );
		canvas.DrawBitmap( resizedSprite , new SKPoint( spriteX , spriteY ) );
		canvas.DrawText( charName , charNameX , charNameY , charAlignment , charFont , charPaint );

		return baseBmp;
	}


	public static void Render() {
		var characters = Char.Load( "Characters" );
		var dialogues = Script.Read( Program.Helper.FromRoot( "script.txt" ) );
		var index = 1;

		if ( _config.shouldnotRender ) {
			Console.WriteLine( "Rendering is disabled by no_render option in config.ini. Goodbye..." );
			return;
		}

		foreach ( var line in dialogues ) {
			if ( !characters.TryGetValue( line.Character , out var character ) )
				continue;

			var timeline = Timeline.Build( line.Text , _config._charsPerSecond );
			var frames = CreateFrames(
				_config.videoWidth ,
				_config.videoHeight ,
				character ,
				line.Text ,
				_config.videoFps ,
				_config._charsPerSecond );
			var audioPath = Audio.GenerateBlipTrack( line.Text , character.Blip , timeline );
			var videoSource = new RawVideoPipeSource( frames ) { FrameRate = _config.videoFps };

			var output = $"{index}.webm";

			if ( audioPath != null ) {
				FFMpegArguments
					.FromPipeInput( videoSource )
					.AddFileInput( audioPath )
					.OutputToFile( output , true , opt => opt
						.WithVideoCodec( "libvpx-vp9" )
						.WithAudioCodec( "libopus" )
						.WithCustomArgument( "-deadline realtime -cpu-used 8 -row-mt 1" )
						.ForceFormat( "webm" ) )
					.ProcessSynchronously();
			}
			else {
				FFMpegArguments
					.FromPipeInput( videoSource )
					.OutputToFile( output , true , opt => opt
						.WithVideoCodec( "libvpx-vp9" )
						.WithCustomArgument( "-deadline realtime -cpu-used 8 -row-mt 1" )
						.ForceFormat( "webm" ) )
					.ProcessSynchronously();
			}

			Console.Write( $"[RENDER] {output}\n" );
			index++;
		}

		Console.WriteLine( $"Done rendering {index - 1} files." );
	}

	private class _config {
		public static readonly bool shouldnotRender = Config.Read<bool>( "no_render" );

		public static readonly int videoWidth = Config.Read<int>( "width" );
		public static readonly int videoHeight = Config.Read<int>( "height" );
		public static readonly int videoFps = Config.Read<int>( "fps" );
		public static readonly float _charsPerSecond = Config.Read<float>( "characters_per_second" );

		public static readonly int textY = Config.Read<int>( "text_y" );
		public static readonly SKTextAlign textAlignment = Config.Read<SKTextAlign>( "text_alignment" );
		public static readonly int textSize = Config.Read<int>( "text_size" );
		public static readonly int textX = Config.Read<int>( "text_x" );
		public static readonly bool textPadding = Config.Read( "text_padding_from_image" , false );

		public static readonly int charTextY = Config.Read<int>( "char_text_y" );
		public static readonly int charTextX = Config.Read<int>( "char_text_x" );
		public static readonly int charTextSize = Config.Read<int>( "char_text_size" );
		public static readonly SKTextAlign charAlignment = Config.Read<SKTextAlign>( "char_text_alignment" );
		public static readonly int charSpritePosY = Config.Read<int>( "char_sprite_y" );
		public static readonly int charSpritePosX = Config.Read<int>( "char_sprite_x" );
		public static readonly int charSpriteResizeY = Config.Read<int>( "char_sprite_resize_y" );
		public static readonly int charSpriteResizeX = Config.Read<int>( "char_sprite_resize_x" );

		public static readonly float amplitude = Config.Read<float>( "amplitude" );
		public static readonly float frequency = Config.Read<float>( "frequency" );
		public static readonly float decay = Config.Read<float>( "decay" );
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
						: currentLine + " " + word;

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

		public static float Bounce( float t ) => 
			(float)( Math.Sin( t * _config.frequency ) * _config.amplitude * Math.Exp( -t * _config.decay ) );
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

	public int Width => Source.Width;
	public int Height => Source.Height;
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