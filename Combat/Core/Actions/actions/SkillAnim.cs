using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Actors;
using Anjin.Nanokin;
using Combat.Data;
using Combat.UI.Notifications;
using Cysharp.Threading.Tasks;
using Data.Combat;
using JetBrains.Annotations;
using Overworld.Cutscenes;
using Pathfinding.Util;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Combat.Toolkit
{
	public class SkillAnim : BattleAnim
	{
		/// <summary>
		/// When loop use action is enabled, this key disables it temporarily by holding.
		/// </summary>
		private const Key LOOP_TOGGLE_HOLD_KEY = Key.Z;

		public BattleSkill skill;
		public bool        animate = true;
		public bool        withUI  = true;

		public override bool Skippable => false;

		//public Fighter protectee { get; private set; }
		public Fighter protector { get; private set; }

		//private Targeting original;

		public SkillAnim(
			[CanBeNull] Fighter fighter,
			BattleSkill         skill,
			Targeting           targeting)
		{
			this.fighter   = fighter;
			this.targeting = targeting;
			this.skill     = skill;
			this.useInfo   = skill;
		}

		public SkillAnim(
			[CanBeNull] Fighter fighter,
			SkillAsset          skill,
			Battle         battle,
			Targeting           targeting)
		{
			this.fighter   = fighter;
			this.targeting = targeting;
			this.skill     = new BattleSkill(battle, skill);
			this.useInfo   = this.skill ;
		}

		//Evaluate the targeting in its entirety to replace any targets involving the protectee with the protector so it takes the damage instead (also checks to make sure the protector won't get victimized twice)
		public void RedirectVictim(Fighter protector)
		{
			//this.protectee = protectee;
			this.protector = protector;
		}

		public override void RunInstant()
		{
			if (!GetUseAction(out BattleAnim skillAnim, out SkillEvent @event))
				return;

			bool hurts    = false;
			bool heals    = false;
			bool physical = false;
			bool magical  = false;

			foreach (Proc proc in procs)
			{
				var hurtingProcs = proc.effects.FindAll(x => x.IsHurting);
				var healingProcs = proc.effects.FindAll(x => x.IsHealing);

				hurts    = (hurtingProcs.Count > 0);
				heals    = (healingProcs.Count > 0);
				magical  = (hurtingProcs.FindAll(x => x.IsMagical).Count > 0);
				physical = (hurtingProcs.FindAll(x => x.IsPhysical).Count > 0);
			}

			@event.hurts    = hurts;
			@event.heals    = heals;
			@event.physical = physical;
			@event.magical  = magical;

			OnBefore(@event, skillAnim);
			skillAnim.RunInstant();
			OnAfter(@event, skillAnim);
		}

		private void OnBefore([NotNull] SkillEvent @event, [NotNull] BattleAnim anim)
		{
			// TODO this is for Beak Brigade cover, but this is not the right place for this functionality
			// @event.TargetEffectList.Clear();
			// foreach (Proc proc in anim.procs)
			// {
			// 	ProcContext ctx = new ProcContext();
			//
			// 	switch (proc.kind)
			// 	{
			// 		case ProcKinds.Fighter:
			// 			foreach (Fighter fter in proc.fighters)
			// 			{
			// 				proc.Victims.Add(fter);
			// 			}
			//
			// 			foreach (Slot slot in proc.slots)
			// 			{
			// 				Fighter owner = slot.owner;
			//
			// 				if (owner != null)
			// 				{
			// 					proc.Victims.Add(owner);
			// 				}
			// 			}
			//
			// 			if (proc.Victims.Count == 0)
			// 			{
			// 				this.LogWarn($"{proc} has no victims to apply on!");
			// 				return;
			// 			}
			//
			// 			break;
			//
			// 		case ProcKinds.Slot:
			// 			foreach (Slot slot in proc.slots) proc.Victims.Add(slot);
			//
			// 			foreach (Fighter fter in proc.fighters)
			// 			{
			// 				if (fter.home != null)
			// 				{
			// 					proc.Victims.Add(fter);
			// 				}
			// 			}
			//
			// 			break;
			//
			// 		default:
			// 			throw new ArgumentOutOfRangeException();
			// 	}
			//
			// 	foreach (object victim in proc.Victims)
			// 	{
			// 		battle.SetupProcContext(proc, ctx, victim);
			// 		@event.TargetEffectList.Add((victim, ctx));
			// 	}
			//
			//
			// }

			battle.Emit(Signals.start_skill, fighter, @event);
			//original = targeting.Clone();
		}

		private void OnAfter([NotNull] SkillEvent @event, [CanBeNull] BattleAnim anim = null)
		{
			//foreach (var target in @event.TargetEffectList)
			//{
			//	Fighter fighter = target.Item1 as Fighter;
			//	Slot    slot    = target.Item1 as Slot;

			//	if ((fighter != null) && (fighter.team != null) && !fighter.team.isPlayer)
			//	{
			//		fighter.actor?.ToggleHealthbarVisibility(false);
			//	}
			//	else if ((slot != null) && (slot.owner != null) && (slot.owner.team != null) && !slot.owner.team.isPlayer)
			//	{
			//		slot.owner.actor?.ToggleHealthbarVisibility(false);
			//	}
			//}

			battle.Emit(Signals.end_skill, fighter, @event);

			//targeting = original.Clone();
			//original = null;
		}

		public override async UniTask RunAnimated()
		{
			do
			{
				if (!GetUseAction(out BattleAnim skillAction, out SkillEvent @event))
					return;

				// UI
				// ----------------------------------------
				if (withUI && skill.asset.ShowNameOnUse && !HasAnimFlag(AnimFlags.Overdrive) && skill.Address != "Skills/endoverdrive")
				{
					// UI decoration
					const float duration = CombatNotifyUI.SKILL_USED_POPUP_DURATION;

					if (!skill.asset.CustomDisplayName)
					{
						CombatNotifyUI.DoSkillUsedPopup(skill.asset.DisplayName, duration).Forget();
					}
					else
					{
						string displayName = skill.DisplayName();
						CombatNotifyUI.DoSkillUsedPopup(displayName, duration).Forget();
					}

					await UniTask.Delay(TimeSpan.FromSeconds(0.2f), cancellationToken: cts.Token).SuppressCancellationThrow();
					if (cts.IsCancellationRequested)
					{
						RunInstant(skillAction);
						return;
					}
				}

				//fighter.NotifyCoach(AnimID.CombatAction);
				if (fighter != null)
				{
					if (fighter.coach != null)
					{
						fighter.coach.SetAction();

						await UniTask.Delay(500);

						if (fighter.coach.actor != null)
						{
							fighter.coach.actor.SignalForAction();

							await UniTask.Delay(500);
						}
					}
					else
					{
						if (fighter.actor != null)
						{
							fighter.actor.SignalForAction();

							await UniTask.Delay(500);
						}
					}
				}

				OnBefore(@event, skillAction);
				if(runner.animConfig.EnableUniversalSkillDelay)
					await UniTask.DelayFrame(runner.animConfig.UniversalSkillDelayFrames);
				await RunAnimated(skillAction);
				OnAfter(@event, skillAction);

				if (GameOptions.current.combat_use_loop)
				{
					runner.camera.SetZero();
					await UniTask.Delay((int)(GameOptions.current.combat_use_loop_delay.Value * 1000));
				}
			} while (GameOptions.current.combat_use_loop && !GameInputs.IsDown(LOOP_TOGGLE_HOLD_KEY));
		}

		private bool HasAnimFlag(string flag)
		{
			return animflags != null && animflags.Contains(flag);
		}

		private bool GetUseAction([CanBeNull] out BattleAnim skillAnim, [CanBeNull] out SkillEvent @event, string action = "")
		{
			skillAnim = null;
			@event      = null;

			if (skill == null)
			{
				DebugLogger.LogError("A participant has attempted to use a null skill! Skipping action...", LogContext.Combat, LogPriority.High);
				return false;
			}

			DebugLogger.Log($"[EFFECT] {fighter} :: use {skill.asset.name} on {targeting.ToStringPicks()}", LogContext.Combat, LogPriority.Low);

			// Pre-use
			// ----------------------------------------
			@event = new SkillEvent(fighter, skill, this);
			if (battle.EmitCancel(Signals.use_skill, fighter, @event))
				return false;

			// Use
			// ----------------------------------------

			skill.battle    = battle;
			skill.user      = fighter;
			skill.targeting = targeting;

			// Deduct the SP cost for the skill.
			if (GameOptions.current.combat_use_cost)
			{
				if (fighter.points.sp <= 0)
					return false;

				battle.AddPoints(fighter, -battle.GetSkillCost(skill));
			}
			else
			{
				DebugLogger.Log("[TRACE] skipping sp cost for skill since 'combat_no_sp_check' is enabled in option.ini.", LogContext.Combat, LogPriority.Low);
			}

			try
			{
				skillAnim = skill.Use();
			}
			catch (Exception e)
			{
				skillAnim = null;
				DebugLogger.LogException(e);
				return false;
			}

			if (skillAnim == null)
			{
				DebugLogger.LogWarning($"Got no action for '{skill.asset.name}'.", LogContext.Combat, LogPriority.High);
				return false;
			}

			return true;
		}

		public override string ToString() => $"{nameof(SkillAnim)}({skill})";
	}
}