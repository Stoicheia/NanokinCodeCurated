using System;
using System.Linq;
using Anjin.Utils;
using Combat.Data;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Overworld.Cutscenes;
using UnityEngine;
using UnityUtilities;
using Util.UniTween.Value;
using Object = UnityEngine.Object;

namespace Combat.Components
{
	/// <summary>
	/// Implements the win/lose condition into the battle.
	/// Win  - All of the monsters in enemy teams have been defeated.
	/// Lose - All of the monsters in the player's team have been defeated.
	/// </summary>
	public class WinLoseConditionRegular : Chip
	{
		/// <summary>
		/// Allows the win to be processed immediately once the last
		/// living fighter is marked dead.
		/// </summary>
		public bool transitionOnFinalDeathMarked = true;

		public float transitionDelay = 0.135f;

		public float transitionReappearDelay = 0.5f;

		private bool _animating;

		public override async UniTask InstallAsync()
		{
			await base.InstallAsync();

			_animating = false;
			if (transitionOnFinalDeathMarked)
			{
				battle.AddTriggerCsharp(OnMarkDead, new Trigger
				{
					ID     = "win-on-last-hit",
					signal = Signals.mark_dead,
					filter = Trigger.FILTER_ANY
				});
			}

			battle.AddTriggerCsharp(OnDeath, new Trigger
			{
				ID     = "win-lose-condition",
				signal = Signals.kill_fighter,
				filter = Trigger.FILTER_ANY
			});
		}

		private void OnMarkDead(TriggerEvent obj)
		{
			if (_animating) return;

			var fighter = (Fighter)obj.me;

			if (fighter.team != null && fighter.team.fighters.All(f => f.deathMarked || f.existence == Existence.Dead))
			{
				_animating = true;
				if (!fighter.team.isPlayer)
					AnimateLastDeath(fighter, CoreOpcode.WinBattle).Forget();
				else
				{
					AnimateLastDeath(fighter, CoreOpcode.LoseBattle).Forget();
				}
			}
		}

		private async UniTaskVoid AnimateLastDeath(Fighter fter, CoreOpcode op)
		{
			// TODO this transition functionality should perhaps be moved elsewhere
			await UniTask.Delay(TimeSpan.FromSeconds(transitionDelay));

			// Fade the screen to a white flash, and dispatch victory
			// ----------------------------------------
			var timescale = TimeScaleVolume.Spawn("Victory Slowdown", runner.arena.gameObject.transform.position);

			//timescale.Tween(0, 1, new EaserTo(0.1f, Ease.Linear));

			runner.screenFade.color = Color.white.Alpha(0);
			await UniTask2.Seconds(0.75f);

			await runner.screenFade.DOFade(1, 0.6f);

			// Hide actor during white flash
			if (fter.actor != null)
				fter.actor.gameObject.SetActive(false);

			Object.Destroy(timescale.gameObject);

			runner.Submit(op);
			runner.CancelAction();
			foreach (var f in fter.team.fighters)
			{
				f.actor.gameObject.SetActive(false);
			}
			//core.WaitForResult();

			await UniTask.Delay(TimeSpan.FromSeconds(transitionReappearDelay)); // TODO this isn't that great, we should be able to wait for the exact moment the other victory transition starts
			runner.screenFade.DOFade(0, 0.6f);

			_animating = false;
		}

		private void OnDeath(TriggerEvent eventbase)
		{
			bool HasLivingFighters(Team team)
			{
				return team.fighters.Count > 0;
			}

			if (battle.teams.Count(HasLivingFighters) == 1)
			{
				Team winteam = battle.teams.First(HasLivingFighters);
				if (winteam.isPlayer)
					runner.Submit(CoreOpcode.WinBattle);
				else
					runner.Submit(CoreOpcode.LoseBattle);
			}
		}
	}
}