using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Combat.Components
{
	public class ArenaIntroMarker : Marker, INotification
	{
		public ArenaIntroEvent EventType;

		public int TeamId = -1;

		public PropertyName id => "ArenaIntroMarker";
	}
}