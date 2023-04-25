using System;
using UnityEngine.Playables;

[Serializable]
public class TimeClipBehaviour : PlayableBehaviour
{
	public EndActions EndAction;

	public enum EndActions
	{
		Nothing,
		Pause,
		Loop,
	}
}