using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class TimeMarker : Marker, INotification
{
	public PropertyName id { get; }

	[InfoBox("The timeline will automatically pause when this marker is reached.")]
	public bool pauses = true;

	[InfoBox("The timeline will automatically jump back to this marker when the next one is reached.")]
	public bool loops = false;

	// [InfoBox("The timeline will automatically jump to this segment when it is reached.")]
	// public string jump = "";
}