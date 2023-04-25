using System.Collections.Generic;
using Anjin.Actors;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;

namespace Anjin.Minigames.Racing
{
	[RequireComponent(typeof(RaceMinigame))]
	public class RacePlaybackRecorder : MonoBehaviour
	{
		[MinValue(0)]
		[MaxValue("RacerIndexMax")]
		[ValidateInput("ValidateRacerIndex")]
		public int RacerIndex = 0;

		private RaceMinigame             _race;
		private Actor                    _savedPlayer;
		private List<RaceMinigame.Racer> _savedRacers;
		private Actor                    _actor;
		private ActorPlayback            _playback;

		private void Awake()
		{
			_race = GetComponent<RaceMinigame>();
		}

		[Button]
		[ShowInPlay]
		[ShowIf("@!IsBusy")]
		public async void StartRecording()
		{
			if (!_race.Racers.IsIndexInBounds(RacerIndex))
			{
				this.LogError("Racer index={RacerIndex} is out of bounds!");
				return;
			}

			if (_race.Racers[RacerIndex].Type != RaceMinigame.RacerType.Playback)
			{
				this.LogError("The racer to edit is not a playback racer!");
				return;
			}

			_savedPlayer = ActorController.playerActor;

			_savedRacers = _race.Racers;
			_race.Racers = new List<RaceMinigame.Racer>();

			for (var i = 0; i < _savedRacers.Count; i++)
			{
				RaceMinigame.Racer orig = _savedRacers[i];

				if (i == RacerIndex)
				{
					_actor = Instantiate(orig.Prefab);

					ActorController.SetPlayer(_actor);

					_playback      = _actor.gameObject.AddComponent<ActorPlayback>();
					_playback.Data = orig.PlaybackData;

					_race.Racers.Add(new RaceMinigame.Racer
					{
						Type       = RaceMinigame.RacerType.Player,
						Reference  = _actor,
						onFinished = OnFinished
					});
				}
				else
				{
					_race.Racers.Add(new RaceMinigame.Racer
					{
						Type = RaceMinigame.RacerType.Dummy
					});
				}
			}

			if (_race.state == MinigameState.Running)
			{
				await _race.Finish();
				_race.CleanupRace();
			}

			_race.onStopTmp  = OnStop;
			_race.onStartTmp = OnStart;
			_race.Begin(MinigamePlayOptions.None).Forget();
		}

		private void OnStart()
		{
			_playback.Record();
		}

		private void OnFinished(RaceMinigame.Racer obj)
		{
			_race.Finish().Forget();
		}

		private void OnStop()
		{
			_playback.Stop();
			_race.CleanupRace();

			ActorController.SetPlayer(_savedPlayer);

			_race.Racers = _savedRacers;
			_savedRacers = null;

			Destroy(_actor.gameObject);
			_actor    = null;
			_playback = null;

			GameEffects.FadeIn(0);
		}

#if UNITY_EDITOR
		[UsedImplicitly]
		private bool IsBusy
		{
			get
			{
				if (_race == null)
					_race = GetComponent<RaceMinigame>();

				return _race.state != MinigameState.Off;
			}
		}

		private int RacerIndexMax
		{
			get
			{
				if (_race == null)
					_race = GetComponent<RaceMinigame>();

				return _race.GetComponent<RaceMinigame>().Racers.Count;
			}
		}

		private bool ValidateRacerIndex()
		{
			if (_race == null)
				_race = GetComponent<RaceMinigame>();

			return _race.Racers.Count > 0 && _race.Racers[RacerIndex].Type == RaceMinigame.RacerType.Playback;
		}
#endif
	}
}