using System;
using UnityEngine;
using UnityEngine.Playables;

namespace Combat.Components
{
	public class ArenaIntroEventHandler : MonoBehaviour, INotificationReceiver
	{
		[NonSerialized]
		public ArenaIntroAnim intro;

		public void OnNotify(Playable origin, INotification notification, object context)
		{
			if (notification is ArenaIntroMarker marker)
			{
				switch (marker.EventType)
				{
					case ArenaIntroEvent.AdvantageNotification:
						intro.ShowAmbushNotification();
						break;

					case ArenaIntroEvent.TeamEntrance:
						intro.EnterNextTeam();
						break;

					case ArenaIntroEvent.EntranceFinish:
						intro.FinishEntrance();
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
	}
}