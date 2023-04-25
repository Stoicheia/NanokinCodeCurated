using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Audio
{
	//Holds different layers of music states to blend between.
	public class GameAudioSequence : SerializedScriptableObject
	{
		public AudioLayer Type;
		public GameAudioSequenceState BaseState;
		public Dictionary<string, GameAudioSequenceState> SubStates;
	}

	//A set of tracks that defines a state of music that could play in game!
	public class GameAudioSequenceState
	{
		public List<AudioClip> Tracks;
	}

}