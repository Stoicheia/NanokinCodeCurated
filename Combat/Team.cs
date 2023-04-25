using System.Collections.Generic;
using System.Linq;
using Anjin.Scripting;
using Anjin.Util;
using Combat.Data;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;

namespace Combat
{
	[LuaUserdata]
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class Team
	{
		private static readonly AutoIncrementInt _nextID = AutoIncrementInt.Zero;

		/// <summary>
		/// Battle that this team is from.
		/// </summary>
		public Battle battle;

		/// <summary>
		/// Whether or not the team belongs to the player.
		/// </summary>
		[ShowInInspector]
		public bool isPlayer;

		/// <summary>
		/// ID of the team.
		/// </summary>
		[ShowInInspector]
		public int id;

		/// <summary>
		/// Brain of the team.
		/// It will be responsible for decisions regarding actions
		/// on each of its fighters' turns.
		/// </summary>
		[CanBeNull]
		[ShowInInspector]
		public BattleBrain brain;

		/// <summary>
		/// All of the fighters currently in this team.
		/// </summary>
		[ShowInInspector]
		public List<Fighter> fighters = new List<Fighter>();

		/// <summary>
		/// All of the fighters in this team that are dead or not considered part of the game.
		/// </summary>
		[ShowInInspector]
		public List<Fighter> deadFighters = new List<Fighter>();

		/// <summary>
		/// The team's slot system.
		/// </summary>
		[ShowInInspector]
		public SlotGrid slots;

		/// <summary>
		/// A single coach that manages the whole team (used for special fights, like Peggie and her three Nanokin in the very first tournament.)
		/// </summary>
		[CanBeNull]
		[HideInInspector]
		public Coach coach;

		private static List<Fighter> _tmpfighters = new List<Fighter>();

		public Team()
		{
			id = _nextID;
		}

		[NotNull]
		public Fighter AddFighter([NotNull] Fighter fighter, bool auslot = true)
		{
			fighters.Add(fighter);
			fighter.team = this;

			if (auslot && fighter.home == null && slots != null)
			{
				battle.SetHome(fighter, slots.GetDefaultSlot(fighters.Count - 1));
			}

			return fighter;
		}

		public void RemoveFighter(Fighter fighter)
		{
			if (fighters.Remove(fighter))
			{
				fighter.team = null;
			}
		}

		public bool HasBuff(string id)
		{
			foreach (Fighter fighter in fighters)
			{
				if (battle.HasState(fighter, id))
					return true;
			}

			return false;
		}

		public int CountBuffs(string id)
		{
			var sum = 0;
			foreach (Fighter fighter in fighters)
			{
				if (battle.HasState(fighter, id))
					sum++;
			}

			return sum;
		}

		public void EnsureFighterHomes()
		{
			for (var i = 0; i < fighters.Count; i++)
			{
				Fighter fighter     = fighters[i];
				Slot    defaultSlot = slots.GetDefaultSlot(i);
				if (fighter.home == null && !defaultSlot.taken)
				{
					battle.SwapHome(fighter, defaultSlot);
				}
			}

			for (var i = 0; i < fighters.Count; i++)
			{
				Fighter fighter = fighters[i];
				if (fighter.home == null)
				{
					battle.SwapHome(fighter, slots.GetRandomFreeSlot());
				}
			}
		}

		public void EnsureCoachPlots()
		{
			for (int i = 0, j = 0; i < fighters.Count; i++)
			{
				Fighter fighter = fighters[i];
				if (fighter.coach != null)
				{
					fighter.coach.home = slots.component.CoachShape.Get(j, fighters.Count);
					j++;
				}
			}
		}

		public void AutoSlots(bool forceAll = true, string shape = null)
		{
			foreach (Fighter fter in fighters)
			{
				if (fter.home == null || forceAll)
					_tmpfighters.Add(fter);
			}


			// Auto shape
			if (shape == null)
				shape = GetShapeForTeamSize(_tmpfighters.Count);

			// Get targets
			List<Target> positionings = shape == null
				? null
				: TargetAPI.shape(battle, null, shape, false, true, slots.all, false);

			// Select nearest to center
			Target pt = null;
			if (positionings != null)
			{
				float minDist = float.MaxValue;
				foreach (Target t in positionings)
				{
					float dist = Vector2.Distance(t.gcenter, slots.center_coord);
					if (dist < minDist)
					{
						minDist = dist;
						pt      = t;
					}
				}
			}

			// Clear homes first
			foreach (Fighter fter in _tmpfighters)
				battle.ClearHome(fter);

			// Then we assign 1-to-1 with the target
			for (var i = 0; i < _tmpfighters.Count; i++)
			{
				Fighter ft = _tmpfighters[i];

				Slot slot = pt != null
					? pt.slots[i]
					: slots.all.SafeGet(i);

				if (slot == null)
					continue;

				battle.SetHome(ft, slot);
			}

			// Cleanup
			_tmpfighters.Clear();
		}

		[CanBeNull]
		public static string GetShapeForTeamSize(int size)
		{
			switch (size)
			{
				case 1:
					return @"
x
							";
				case 2:
					return @"
x
x
							";
				case 3:
					return @"
x
x
x
							";
				case 4:
					return @"
x
xx
x
							";
				case 5:
					return @"
x
xxx
x
							";
				case 6:
					return @"
xx
xx
xx
";
				case 7:
					return @"
xx
xxx
xx
";
				case 8:
					return @"
xxx
xxx
xx
";
				case 9:
					return @"
xxx
xxx
xxx
";
			}

			return null;
		}

		public void AddDead(Fighter fter)
		{
			deadFighters.Add(fter);
			fighters.Remove(fter);
		}
	}
}