using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Combat.Components.VictoryScreen.Menu
{
	public class ArenaVictoryMarker : Marker, INotification
	{
		public ArenaVictoryEvent EventType;

		public PropertyName id { get; }
	}
}