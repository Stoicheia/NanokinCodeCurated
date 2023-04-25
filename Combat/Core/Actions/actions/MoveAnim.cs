using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Scripting;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace Combat.Toolkit
{
	[LuaEnum("movements", StringConvertible = true)]
	public enum MoveSemantic
	{
		/// <summary>
		/// Automatically get the semantic for the context.
		/// Typically this is used as a default for arguments and parameters
		/// where it will use the fighter's own move semantic.
		/// </summary>
		Auto,

		/// <summary>
		/// The fighter stays in contact with the ground the whole time.
		/// </summary>
		Ground,

		/// <summary>
		/// The fighter moves in the air, and remains in the air.
		/// </summary>
		Aerial,

		/// <summary>
		/// The fighter moves in the air, but lands on the ground at the end of the movement.
		/// </summary>
		Landing,

		/// <summary>
		/// The fighter moves through instant flash teleportation, bypassing slot logic.
		/// Also ignores obstacles during targeting in certain contexts.
		/// </summary>
		Teleport
	}

	public static class MoveSemanticExtensions
	{
		public static bool IsAffectedByIce(this MoveSemantic sem)
		{
			switch (sem)
			{
				case MoveSemantic.Auto:
					DebugLogger.LogError("Attempting to use MoveSemantic.Default for ice physics. The default semantic should have been replaced by now.", LogContext.Overworld, LogPriority.Low);
					return true;

				case MoveSemantic.Ground:   return true;
				case MoveSemantic.Aerial:   return false;
				case MoveSemantic.Landing:  return true;
				case MoveSemantic.Teleport: return false;
				default:
					throw new ArgumentOutOfRangeException(nameof(sem), sem, null);
			}
		}
	}

	public class MoveAnim : BattleAnim
	{
		private readonly Slot         _slot;
		private readonly MoveSemantic _semantic;

		public Slot Slot => _slot;

		public MoveAnim(Fighter fighter, Slot slot, MoveSemantic semantic = MoveSemantic.Auto)
		{
			this.fighter = fighter;
			_slot        = slot;
			_semantic    = semantic;
		}

		public override void RunInstant()
		{
			Assert.IsNotNull(fighter, $"{nameof(fighter)} != null");
			battle.SwapHome(fighter, _slot);
		}

		public override async UniTask RunAnimated()
		{
			var moveTasks = new List<UniTask>();

			Assert.IsNotNull(fighter, $"{nameof(fighter)} != null");
			Battle.SlotSwap userSwap = battle.SwapHome(fighter, _slot, _semantic);

			//fighter.NotifyCoach(AnimID.CombatAction);
			//if ((fighter != null) && (fighter.coach != null))
			//{
			//	fighter.coach.SetAction();
			//}

			moveTasks.Add(RunAnimated(new CoplayerAnim("action-move", "move_self", fighter)));
			if (userSwap.swapee.HasValue)
				moveTasks.Add(RunAnimated(new CoplayerAnim("action-move", "move_self", userSwap.swapee.Value.fighter)));

			await UniTask.WhenAll(moveTasks);
		}
	}
}