using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Scripting;
using Anjin.Util;
using Combat.Data;
using Combat.UI;
using Cysharp.Threading.Tasks;
using Data.Combat;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace Combat.Components
{
	/// <summary>
	/// Handles the basic animation features of combat
	///
	/// - Spawns in damage numbers
	/// - Displays misses
	/// - Default move animations for procs
	/// </summary>
	public class ProcAnimator : Chip
	{
		private const string ANIMATION_SCRIPT = "action-move";

		private Table _moveEnv;

		private struct ActorState
		{
			public readonly ActorBase actor;
			public          int       numberCircleIndex;

			public ActorState(ActorBase actor)
			{
				this.actor        = actor;
				numberCircleIndex = 0;
			}
		}

		private List<ActorState> _actorStates = new List<ActorState>();

		public override async UniTask InstallAsync()
		{
			await base.InstallAsync(); //

			battle.ProcApplied          += OnProcApplied;
			battle.ProcMissed           += OnProcMissed;
			battle.FighterPointsChanged += OnFighterPointsChanged;

			_actorStates.Clear();
			_moveEnv = Lua.NewScript(ANIMATION_SCRIPT);
			LuaUtil.LoadBattleRequires(_moveEnv);
		}

		private void OnProcApplied([NotNull] ProcContext appli)
		{
			Proc proc = appli.proc;

			bool animatedSwap   = false;
			bool animatedVictim = false;

			appli.AddImplicitAnimations();

			foreach (ProcAnimation anim in appli.animators)
			{
				this.LogVisual("--", $"dispatching animation anim={anim} ...");

				Coplayer player = Lua.RentPlayer(true);
				Fighter  me     = null;

				proc.onAnimating?.Invoke(appli, player, anim);

				switch (anim.type)
				{
					case ProcAnimation.Type.Proc:
						// Currently not needed for anything, so it's left unimplemented
						throw new NotImplementedException();

					case ProcAnimation.Type.Victim:
						// player.baseState.SetFighterSelf(appli.victim); // Animate the victim of the proc
						player.Play(proc.GetEnv().table, anim.closure, new object[] { appli.victim });
						animatedVictim = true;
						break;

					case ProcAnimation.Type.Swapee:
						foreach (Battle.SlotSwap swap in appli.resultingFormationSwaps)
						{
							if (swap.swapee.HasValue)
							{
								// player.baseState.SetFighterSelf(swap.swapee.Value.fighter); // Animate the swappee
								player.Play(proc.GetEnv().table, anim.closure, new object[] { appli.victim });
							}
						}

						animatedSwap = true;
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			if (appli.resultingFormationSwaps.Count > 0 && (!animatedSwap || !animatedVictim))
			{
				foreach (Battle.SlotSwap swap in appli.resultingFormationSwaps)
				{
					if (!animatedVictim)
					{
						Coplayer player = Lua.RentPlayer(true);
						// player.baseState.SetFighterSelf(swap.me.fighter);
						player.Play(_moveEnv, "move_self", new object[] { swap.me.fighter });
					}

					if (!animatedSwap && swap.swapee.HasValue)
					{
						Coplayer player = Lua.RentPlayer(true);
						// player.baseState.SetFighterSelf(swap.swapee.Value.fighter);
						player.Play(_moveEnv, "move_other", new object[] { swap.swapee.Value.fighter });
					}
				}
			}
		}

		private void OnProcMissed([NotNull] ProcContext appli) { }

		private void OnFighterPointsChanged(Fighter fighter, PointChange chg)
		{
			Pointf v = chg.value;

			if (chg.noNumbers)
				return;

			if (chg.miss)
			{
				// Show "MISS".
				DamageNumber prefab = GetMissDamageNumberPrefab();
				//DamageNumber damageNumber = Object.Instantiate(prefab, RandomizeDamageNumberPosition(fighter.actor.transform.position), fighter.actor.transform.rotation);
				DamageNumber damageNumber = Object.Instantiate(prefab, RandomizeDamageNumberPosition(fighter.actor), fighter.actor.transform.rotation);

				return;
			}

			if (chg.critical)
			{
				// SHOW critical effects (cool sound, colored/different numbers maybe.
			}

			if (v.hp < 0) //
			{
				DamageNumber prefab = GetDamageNumberPrefab(chg.element);
				//DamageNumber damageNumber = Object.Instantiate(prefab, RandomizeDamageNumberPosition(fighter.actor.transform.position), fighter.actor.transform.rotation);
				DamageNumber damageNumber = Object.Instantiate(prefab, RandomizeDamageNumberPosition(fighter.actor), fighter.actor.transform.rotation);
				damageNumber.SetDamageNumber((int)v.hp.Abs());

				if (chg.element != Elements.none)
				{
					ResistanceBracket bracket = Elementf.GetBracket(fighter.info.Resistances[chg.element]);

					if (bracket != ResistanceBracket.Norm)
					{
						EfficiencyText effPrefab      = runner.animConfig.efficiencyText;
						EfficiencyText efficiencyText = Object.Instantiate(effPrefab, fighter.actor.transform.position + Vector3.up * 2, fighter.actor.transform.rotation);
						efficiencyText.SetLabelText(bracket);
					}
				}
			}
			else if (v.hp > 0)
			{
				DamageNumber damageNumber = Object.Instantiate(runner.animConfig.healNumber, fighter.actor.transform.position, fighter.actor.transform.rotation);
				damageNumber.SetDamageNumber((int)v.hp);

				if (chg.element != Elements.none)
				{
					ResistanceBracket bracket = Elementf.GetBracket(fighter.info.Resistances[chg.element]);

					if (bracket != ResistanceBracket.Norm)
					{
						EfficiencyText effPrefab      = runner.animConfig.efficiencyText;
						EfficiencyText efficiencyText = Object.Instantiate(effPrefab, fighter.actor.transform.position + Vector3.up * 2, fighter.actor.transform.rotation);
						efficiencyText.SetLabelText(bracket);
					}
				}
			}
			else if (v.sp < 0)
			{
				DamageNumber damageNumber = Object.Instantiate(runner.animConfig.damageNumberPure, RandomizeDamageNumberPosition(fighter.actor.transform.position), fighter.actor.transform.rotation);
				damageNumber.SetDamageNumber((int)v.sp.Abs());
			}
			else if (v.sp > 0)
			{
				DamageNumber damageNumber = Object.Instantiate(runner.animConfig.healNumberSP, RandomizeDamageNumberPosition(fighter.actor.transform.position), fighter.actor.transform.rotation);
				damageNumber.SetDamageNumber((int)v.sp);
			}


			HealthbarUI.EnableTimed(fighter);
		}

		private DamageNumber GetMissDamageNumberPrefab()
		{
			return runner.animConfig.miss;
		}

		private DamageNumber GetDamageNumberPrefab(Elements element)
		{
			switch (element)
			{
				case Elements.none:   return runner.animConfig.damageNumberPure;
				case Elements.blunt:  return runner.animConfig.damageNumberBlunt;
				case Elements.pierce: return runner.animConfig.damageNumberPierce;
				case Elements.slash:  return runner.animConfig.damageNumberSlash;
				case Elements.gaia:   return runner.animConfig.damageNumberGaia;
				case Elements.oida:   return runner.animConfig.damageNumberOida;
				case Elements.astra:  return runner.animConfig.damageNumberAstra;
				default:
					return runner.animConfig.damageNumberPure;
			}
		}

		private static Vector3 RandomizeDamageNumberPosition(Vector3 position) => position + new Vector3(Random.Range(-0.5f, 0.5f), 0f, Random.Range(0f, -0.25f));

		private Vector3 RandomizeDamageNumberPosition([NotNull] ActorBase actor)
		{
			const float circle_steps = 10f;
			const float radius       = 0.75f;
			const float noise        = 0.2f;
			const float golden_num   = 1.618033988749895f;

			Vector3 center = actor.center;
			for (int i = 0; i < _actorStates.Count; i++)
			{
				ActorState actstate = _actorStates[i];
				if (actstate.actor == actor)
				{
					actstate.numberCircleIndex++;
					_actorStates[i] = actstate;
					float t = actstate.numberCircleIndex / circle_steps;
					return center
					       + RNG.InCircle * noise
					       + GetCirclePos(t * golden_num * 5, radius);
				}
			}

			_actorStates.Add(new ActorState(actor));
			return RandomizeDamageNumberPosition(actor);
		}

		/// <summary>
		/// Get the position on a circle from origin around a radius.
		/// </summary>
		/// <param name="t">t in range 0 to 1</param>
		/// <param name="radius">The radius of the circle</param>
		/// <returns>Point on the circumferance of the circle.</returns>
		private static Vector3 GetCirclePos(float t, float radius) => new Vector3(Mathf.Cos(t * Mathf.PI * 2f) * radius, 0f, Mathf.Sin(t * Mathf.PI * 2f) * radius);
	}
}