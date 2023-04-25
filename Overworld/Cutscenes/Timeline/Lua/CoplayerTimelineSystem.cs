using System;
using System.Reflection;
using Anjin.Scripting;
using Anjin.Scripting.Waitables;
using Anjin.Util;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Util.Odin.Attributes;

namespace Overworld.Cutscenes.Timeline
{
	public class CoplayerTimelineSystem : MonoBehaviour, INotificationReceiver
	{
		public PlayableDirector Director;
		public Coplayer         Owner;

		public Marker             LastMarker;
		public bool               Paused;
		public ICoroutineWaitable PausingWaitable;

		private void Awake()
		{
			Director = GetComponent<PlayableDirector>();

			Director.stopped += director => {
				LastMarker    = null;

			};
			Director.played  += director => { LastMarker = null; };
		}

		[UsedImplicitly]
		public void OnNotify(Playable origin, INotification notification, object context)
		{
			if (notification is Marker marker)
			{
				if (marker == LastMarker) return;
				LastMarker = marker;
			}

			if (Owner != null)
			{
				Owner.OnTimelineProxyNotification(this, origin, notification, context);
			}
		}

		public void PauseOnMarker(Marker marker, ICoroutineWaitable waitable)
		{
			Director.Pause();
			Paused          = true;
			LastMarker      = marker;
			PausingWaitable = waitable;
		}

		private void Update()
		{
			if (Paused)
			{
				if (PausingWaitable == null || PausingWaitable.CanContinue(false))
				{
					PausingWaitable = null;
					Director.Resume();
					Paused = false;
				}
			}
		}

		public void ResetDirector()
		{
			if(Director.extrapolationMode == DirectorWrapMode.None) {
				Director.time = 0;
				Director.Evaluate();
				Director.gameObject.SetActive(false);
				Director.gameObject.SetActive(true);
			}
		}

		[ShowInPlay]
		public void Evaluate()
		{
			/*var asset = Director.playableAsset as TimelineAsset;
			if (asset) {
				foreach (PlayableBinding binding in asset.outputs) {
					if (binding.sourceObject is ActivationTrack activation) {
						TimelineClip[] clips = typeof(ActivationTrack).GetProperty("clips", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy).GetValue(activation) as TimelineClip[];
						Debug.Log(clips);
					}
				}
			}*/


			//if(Director) Director.Evaluate();
		}
	}
}