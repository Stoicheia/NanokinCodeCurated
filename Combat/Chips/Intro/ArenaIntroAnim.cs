using System;
using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Nanokin.Park;
using Anjin.Util;
using Cinemachine;
using Combat.Data;
using Combat.Toolkit;
using Combat.UI.Notifications;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Serialization;
using Util.Math.Splines;

namespace Combat.Components
{
	public class ArenaIntroAnim : BattleAnim, ICamController
	{
		private const int                  VCAM_PRIORITY = 150;
		public        ArenaIntroProperties properties;

		private PlayableDirector _director;
		private bool             _active;
		private int              _nextTeamIndex;
		private bool             _ambushPlayed;

		public override bool Skippable => true;

		public override async UniTask RunAnimated()
		{
			if (battle.arena == null)
			{
				this.LogError("Cannot play intro without an arena.");
				return;
			}

			if (GameOptions.current.combat_intro && battle.arena.IntroParams)
			{
				properties = battle.arena.IntroParams;

				_director      = battle.arena.IntroParams.RootObject.GetOrAddComponent<PlayableDirector>();
				_director.time = 0;
				_director.Stop();

				GameCams.Push(this);

				// Move all the team members to their spawn plots.
				foreach (SpawnEntry spawnEntry in properties.TeamSpawns)
				{
					SlotLayout teamShape  = spawnEntry.TeamShape;
					PlotShape  spawnShape = spawnEntry.SpawnShape;
					Team       team       = null;

					foreach (Team t in battle.teams)
					{
						if (t.slots.component == teamShape)
						{
							team = t;
							break;
						}
					}

					for (var i = 0; i < team?.fighters.Count; i++)
					{
						Fighter fter = team.fighters[i];
						fter.actor.transform.position = spawnShape.Get(i, team.fighters.Count).position;
					}
				}

				_director.stopped += OnDirectorStopped;
				_director.InjectCinemachineBrain();
				_director.Play();

				_active = true;
				await UniTask.WaitUntil(() => !_active || cts.IsCancellationRequested);

				if (cts.IsCancellationRequested && gracefulCancelation)
				{
					await GameEffects.FadeOut(0.25f);

					_director.time = _director.playableAsset.duration;
					_director.Stop();
					await UniTask.Delay(125);

					properties.VCam.Priority = -1;
					await GameEffects.FadeIn(0.25f);
				}

				_director.stopped -= OnDirectorStopped;

				properties.VCam.Priority = -1;
			}

			ShowAmbushNotification();

			// Reset
			_ambushPlayed = false;
			_active       = false;
			_director     = null;
			properties    = null;
		}

		private void OnDirectorStopped(PlayableDirector obj)
		{
			obj.stopped -= OnDirectorStopped;
			FinishEntrance();
		}

		public void ShowAmbushNotification()
		{
			if (_ambushPlayed) return;
			_ambushPlayed = true;

			if (runner.io.advantage == EncounterAdvantages.Enemy)
			{
				AmbushNotify.Play();
			}
		}

		public void EnterNextTeam()
		{
			SpawnEntry entry = properties.TeamSpawns[_nextTeamIndex++];

			SlotLayout teamShape  = entry.TeamShape;
			PlotShape  spawnShape = entry.SpawnShape;

			Team team = battle.GetTeam(teamShape);
			if (team == null)
			{
				this.LogError($"Couldn't find a team assigned to {entry.TeamShape}.");
				return;
			}

			foreach (Fighter fter in team.fighters)
			{
				Slot homeSlot = fter.home;
				if (homeSlot == null)
				{
					Debug.LogWarning("BattleIntroAnimation: An fighter has no home slot and cannot be used for the animation. Weird...");
				}

				// TODO Animate this with Lua
				// spawningEntity.View.FadeColorTo(Color.white, 0.6f);
				// spawningEntity.entity.vfx.Add(new FadeInVFX(0.6f));
				// AnimationTracker.Add(new AnimationContext(spawningEntity).AnimateWith(
				// new MoveAnimation().To(homeSlot.position),
				// new PlayAnimation("idle", PlayOptions.RandomStart)
				// ));
			}
		}

		public void FinishEntrance()
		{
			_active = false;
			GameCams.Pop();
		}

		public void OnActivate()
		{
			properties.VCam.Priority = GameCams.PRIORITY_ACTIVE;
		}

		public void OnRelease(ref CinemachineBlendDefinition? blend) { }

		public void ActiveUpdate() { }

		public void GetBlends(ref CinemachineBlendDefinition? blend, ref CinemachineBlenderSettings settings) { }

		[Serializable]
		public class SpawnEntry
		{
			[FormerlySerializedAs("spawnPlots"), SerializeField, UsedImplicitly]
			public PlotShape SpawnShape;

			[FormerlySerializedAs("TeamSlots"), FormerlySerializedAs("teamSlots"), SerializeField]
			public SlotLayout TeamShape;
		}
	}
}