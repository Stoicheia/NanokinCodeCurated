using System;
using Anjin.Nanokin;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Playables;

namespace Combat.Components
{
	// TODO maybe hook this up to some reusable UI in the corner of the screen, changing it to a hold behavior and an expanding circle or something
	public class PlayableDirectorSkipper : MonoBehaviour
	{
		[NonSerialized]
		public bool temporary;

		private PlayableDirector _director;
		private bool             _skipped = false;

		private void Awake()
		{
			_director         =  GetComponent<PlayableDirector>();
			_director.played  += OnReset;
			_director.stopped += OnReset;
			_director.stopped += OnDirectorStopped;
		}

		private void OnDirectorStopped(PlayableDirector obj)
		{
			if (temporary)
				Destroy(this);
		}

		private void OnReset(PlayableDirector obj)
		{
			_skipped = false;
		}

		public void Update()
		{
			// no PlayableDirector.started event, laughable
			if (GameInputs.confirm.IsPressed && !_skipped)
			{
				Skip().Forget();
				_skipped = true;
			}
		}

		private async UniTask Skip()
		{
			await GameEffects.FadeOut(0.125f);

			if (_director.state == PlayState.Playing) // are we still playing?
			{
				_director.time = _director.playableAsset.duration;
				_director.Stop();
			}

			await GameEffects.FadeIn(0.125f);
		}
	}
}