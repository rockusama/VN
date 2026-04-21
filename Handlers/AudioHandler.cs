using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VN.Handlers;

/// <summary>
///     KNOWN ISSUES
///     DOES NOT SKIP EMOTICONS LIKE :3
///		DO WE NEED TO MAKE A BREATHING?
/// </summary>
public class AudioHandler {
	public static  bool  IsUnpronounceable( char c ) => @"@#$%^&*()-=+[]';/\|`~<>,.!?№:".Contains( char.ToLowerInvariant( c ) );
	private static float minPitch      = Config.Read<float>( "minimum_pitch" );
	private static float maxPitch      = Config.Read<float>( "maximum_pitch" );
	private static float pitchModifier = Config.Read<float>( "pitch_modifier" );
	
	public static string GenerateBlipTrack(
		string text ,
		string blipPath ,
		List<double> timeline
	) {
		var sampleRate = 44100;
		var rand = new Random();
		var output = new List<float>();
		
		for ( var idx = 0 ; idx < text.Length ; idx++ ) {
			var c = text[idx];
			var time = timeline[idx];

			if ( char.IsWhiteSpace( c )
			     || IsUnpronounceable( c )
			   )
				continue;

			var pitch = minPitch + (float)rand.NextDouble() * pitchModifier;
			pitch = Math.Clamp( pitch , minPitch , maxPitch );

			using var reader = new AudioFileReader( blipPath );
			var provider = reader.ToSampleProvider();
			var pitched = new WdlResamplingSampleProvider( provider , (int)( sampleRate * pitch ) );

			var startSample = (int)( time * sampleRate );

			while ( output.Count < startSample )
				output.Add( 0f );

			var buffer = new float[1024];
			int read;
			var writeIndex = startSample;

			while ( ( read = pitched.Read( buffer , 0 , buffer.Length ) ) > 0 ) {
				for ( var i = 0 ; i < read ; i++ ) {
					if ( writeIndex >= output.Count )
						output.Add( buffer[i] );
					else
						output[writeIndex] += buffer[i];

					writeIndex++;
				}
			}
		}

		var path = Path.GetTempFileName() + ".wav";

		using var writer = new WaveFileWriter( path , new WaveFormat( sampleRate , 1 ) );
		foreach ( var s in output )
			writer.WriteSample( Math.Clamp( s , -1f , 1f ) );

		return path;
	}
}