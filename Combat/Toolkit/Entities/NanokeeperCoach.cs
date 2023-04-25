using Anjin.Actors;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Combat
{
	public class NanokeeperCoach : Coach
	{
		public GameObject Prefab;

		private HopVFX hop;

		private int alive;

		private AnimID idleID;
		private AnimID actionID;
		private AnimID hurtID;
		private AnimID winID;
		private AnimID winGoofID;
		private AnimID lossID;

		private List<Fighter> summoned;

		public NanokeeperCoach(Fighter fighter, GameObject prefab) : base(fighter)
		{
			summoned = new List<Fighter>();

			if (fighter != null)
			{
				summoned.Add(fighter);

				alive = 1;
			}

			Prefab = prefab;

			hop = new HopVFX(1, 0.17f, 0.5f, null, null, null);

			NanokeeperCoachActor component = prefab.GetComponentInChildren<NanokeeperCoachActor>(true);

			if (component != null)
			{
				idleID = component.Idle;
				actionID = component.Action;
				hurtID = component.Hurt;
				winID = component.Win;
				winGoofID = component.WinGoof;
				lossID = component.Loss;
			}
		}

		/// <summary>
		/// Add a fighter to a list that's used to determine how many Nanokin are left on the field and pick while idle animation state gets played
		/// </summary>
		/// <param name="fighter"></param>
		public void AddSummoned(Fighter fighter)
		{
			summoned.Add(fighter);

			++alive;
		}

		public override void SetIdle()
		{
			int healthy = 0;

			foreach (Fighter fighter in summoned)
			{
				if (!fighter.deathMarked && (fighter.existence != Existence.Dead) && (fighter.hp_percent > WEAK_PERCENT))
				{
					++healthy;
				}
			}

			if ((healthy > 0) && (idleID != AnimID.None))
				SetAnim(idleID);
			else if ((alive == 0) && (lossID != AnimID.None))
				SetAnim(hurtID);
			else if (hurtID != AnimID.None)
				SetAnim(hurtID);
		}

		public override void OnPointsChanged()
		{
			if ((state == idleID) || (state == hurtID))
			{
				SetIdle();
			}

			//switch (state)
			//{
			//	case AnimID.Stand:
			//	case AnimID.Sit:
			//		SetIdle();
			//		break;
			//}
		}

		public override void OnDeath()
		{
			--alive;

			if (alive < 0)
			{
				alive = 0;
			}

			if ((state == idleID) || (state == hurtID))
			{
				SetIdle();
			}
		}

		public override void OnRevive()
		{
			++alive;

			if (alive > summoned.Count)
			{
				alive = summoned.Count;
			}

			if ((state == idleID) || (state == hurtID) || (state == lossID))
			{
				SetIdle();
			}
		}

		public override void SetAnim(AnimID id)
		{
			if ((actor != null) && (id != AnimID.None))
			{
				if ((id != AnimID.CombatAction) && (id != AnimID.CombatAction2) && (id != AnimID.CombatAction3))
				{
					actor.SetAnim(id);
				}
				else
				{
					if (actionID != AnimID.None)
					{
						actor.SetAnim(actionID);
					}
					else
					{
						HopForAction().Forget();
					}
				}
			}
		}

		private async UniTask HopForAction()
		{
			SetAnim(idleID);

			if (!actor.vfx.Contains(hop))
			{
				actor.vfx.Add(hop);
			}

			await UniTask.Delay(300);

			if (actor.vfx.Contains(hop))
			{
				actor.vfx.Remove(hop);
			}

			SetIdle();
		}

		public override void SetAction()
		{
			SetAnim(actionID);
		}

		public override void SetTurn() {}

		public override void SetWin(){}
	}
}
