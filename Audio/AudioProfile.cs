using System.Collections.Generic;
using Sirenix.OdinInspector;


namespace Anjin.Audio
{
	/// <summary>
	/// A profile of tracks that are enabled to play in a level through the audio controller, while being overridable.
	/// </summary>
	public class AudioProfile : SerializedScriptableObject
	{
		public List<AudioTrack> Tracks;
	}


}