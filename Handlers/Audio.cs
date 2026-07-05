using System.Diagnostics;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace VN.Handlers;

/// <summary>
///     KNOWN ISSUES
///     DOES NOT SKIP EMOTICONS LIKE :3
///     DO WE NEED TO MAKE A BREATHING?
/// </summary>
public static class Audio {
	public static string GenerateBlipTrack(
		string text ,
		string blipPath ,
		List<double> timeline
	) {
		var watch = Stopwatch.StartNew();

		if ( blipPath == null || !File.Exists( blipPath ) )
			return null;

		var sampleRate = 44100;
		var rand = new Random();
		var output = new List<float>();

		using var reader = new AudioFileReader( blipPath );

		for ( var idx = 0 ; idx < text.Length ; idx++ ) {
			var c = text[idx];
			var time = timeline[idx];

			if ( char.IsWhiteSpace( c ) || _config.IsUnpronounceable( c ) )
				continue;

			reader.Position = 0;

			var pitch = _config.minPitch + (float)rand.NextDouble() * _config.pitchModifier;
			pitch = Math.Clamp( pitch , _config.minPitch , _config.maxPitch );

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

		watch.Stop();
		Program.Helper.Log( "Audio took " + watch.Elapsed );

		return path;
	}

	public class _config {
		public static readonly float minPitch = Config.Read<float>( "minimum_pitch" );
		public static readonly float maxPitch = Config.Read<float>( "maximum_pitch" );
		public static readonly float pitchModifier = Config.Read<float>( "pitch_modifier" );
		public static bool IsUnpronounceable( char c ) => @"@#$%^&*()-=+[]';/\|`~<>,.!?№:".Contains( char.ToLowerInvariant( c ) );
	}
}