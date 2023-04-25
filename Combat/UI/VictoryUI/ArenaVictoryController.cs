using System;
using Cinemachine;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Combat.Components.VictoryScreen.Menu
{
	public class ArenaVictoryController : MonoBehaviour, INotificationReceiver
	{
		[NonSerialized]
		public VictoryUI ui;

		[SerializeField] public TimelineAsset            Timeline;
		[SerializeField] public CinemachineVirtualCamera Camera;
		[SerializeField] public PlayableDirector         Director;

		public float ScreenUIEntrance;
		public float ScreenUIOpacity;
		public float CharacterUIOpacity;
		public float CharacterUIEntrance;
		public float FightersOpacity;

		public void OnNotify(Playable origin, INotification notification, object context)
		{
			if (notification is ArenaVictoryMarker marker)
			{
				switch (marker.EventType)
				{
					case ArenaVictoryEvent.BeginDistribution:
						ui.enableDistribution = true;
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
	}
}