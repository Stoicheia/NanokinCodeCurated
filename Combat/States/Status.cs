using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Data.Combat;
using JetBrains.Annotations;
using Sirenix.OdinInspector;

namespace Combat.Data
{
	/// <summary>
	/// A status is the combined list of all states on a IStatee.
	/// </summary>
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public class Status
	{
		/// <summary>
		/// Current states.
		/// </summary>
		public List<State> states = new List<State>();

		public List<StateCmd>   cmds    = new List<StateCmd>(32);
		public List<StateFunc>  funcs   = new List<StateFunc>(16);
		public List<StateEntry> entries = new List<StateEntry>();

		// Stats ----------------------------------------
		[ShowInInspector] public ModStat<Pointf>   points;
		[ShowInInspector] public ModStat<Statf>    stat;
		[ShowInInspector] public ModStat<Elementf> resistance, atk,       def;
		[ShowInInspector] public ModStat<float>    usecost,    skillcost, stickercost;
		[ShowInInspector] public List<EngineFlags> engineFlags;

		private List<RemovedState> _lastRemovedUIDs = new List<RemovedState>();

		public Status()
		{
			Clear();
		}

		public int Count => states.Count;

		public State this[int i]
		{
			get => states[i];
			set => states[i] = value;
		}

		public void Clear()
		{
			// TODO switch to zeroes
			cmds.Clear();
			funcs.Clear();
			entries.Clear();
			points      = ModStatExt.identity_pt;
			stat        = ModStatExt.identity_stat;
			resistance  = ModStatExt.identity_elem;
			atk         = ModStatExt.identity_elem;
			def         = ModStatExt.identity_elem;
			skillcost   = ModStatExt.identity;
			usecost     = ModStatExt.identity;
			stickercost = ModStatExt.identity;
			engineFlags = new List<EngineFlags>();
		}

		public int get_removed_state_uid(string id)
		{
			for (int i = 0; i < _lastRemovedUIDs.Count; i++)
			{
				if (_lastRemovedUIDs[i].id == id)
				{
					return _lastRemovedUIDs[i].uid;
				}
			}

			return -1;
		}

		public void Add(StatOp op, Statf stats)
		{
			if (stats.power > 0) Add(StateStat.power, op, stats.power);
			if (stats.speed > 0) Add(StateStat.speed, op, stats.speed);
			if (stats.will > 0) Add(StateStat.will, op, stats.will);
		}

		public void Add(StatOp op, Elementf res)
		{
			if (res.blunt > 0) Add(StateStat.res_blunt, op, res.blunt);
			if (res.slash > 0) Add(StateStat.res_slash, op, res.slash);
			if (res.pierce > 0) Add(StateStat.res_pierce, op, res.pierce);
			if (res.gaia > 0) Add(StateStat.res_gaia, op, res.gaia);
			if (res.astra > 0) Add(StateStat.res_astra, op, res.astra);
			if (res.oida > 0) Add(StateStat.res_oida, op, res.oida);
		}

		public void Add(StatOp op, Pointf points)
		{
			if (points.hp > 0) Add(StateStat.hp, op, points.hp);
			if (points.sp > 0) Add(StateStat.sp, op, points.sp);
			if (points.op > 0) Add(StateStat.op, op, points.op);
		}

		public void Add(StateStat stat, StatOp op, float val)
		{
			Add(new StateCmd(op, stat, val));
		}

		public void Add(StateCmd cmd)
		{
			switch (cmd.stat)
			{
				case StateStat.points:
					cmd.Apply(ref points.flat, ref points.scale);
					break;

				case StateStat.hp:
					cmd.Apply(ref points.flat.hp, ref points.scale.hp);
					break;

				case StateStat.sp:
					cmd.Apply(ref points.flat.sp, ref points.scale.sp);
					break;

				case StateStat.op:
					cmd.Apply(ref points.flat.op, ref points.scale.op);
					break;

				// ----------------------------------------

				case StateStat.stats:
					cmd.Apply(ref stat.flat, ref stat.scale);
					break;

				case StateStat.power:
					cmd.Apply(ref stat.flat.power, ref stat.scale.power);
					break;

				case StateStat.speed:
					cmd.Apply(ref stat.flat.speed, ref stat.scale.speed);
					break;

				case StateStat.will:
					cmd.Apply(ref stat.flat.will, ref stat.scale.will);
					break;

				// ----------------------------------------

				case StateStat.res:
					cmd.Apply(ref resistance.flat, ref resistance.scale);
					break;

				case StateStat.res_blunt:
					cmd.Apply(ref resistance.flat.blunt, ref resistance.scale.blunt);
					break;

				case StateStat.res_slash:
					cmd.Apply(ref resistance.flat.slash, ref resistance.scale.slash);
					break;

				case StateStat.res_pierce:
					cmd.Apply(ref resistance.flat.pierce, ref resistance.scale.pierce);
					break;

				case StateStat.res_gaia:
					cmd.Apply(ref resistance.flat.gaia, ref resistance.scale.gaia);
					break;

				case StateStat.res_oida:
					cmd.Apply(ref resistance.flat.oida, ref resistance.scale.oida);
					break;

				case StateStat.res_astra:
					cmd.Apply(ref resistance.flat.astra, ref resistance.scale.astra);
					break;

				// ----------------------------------------

				case StateStat.atk:
					cmd.Apply(ref atk.flat, ref atk.scale);
					break;

				case StateStat.atk_blunt:
					cmd.Apply(ref atk.flat.blunt, ref atk.scale.blunt);
					break;

				case StateStat.atk_slash:
					cmd.Apply(ref atk.flat.slash, ref atk.scale.slash);
					break;

				case StateStat.atk_pierce:
					cmd.Apply(ref atk.flat.pierce, ref atk.scale.pierce);
					break;

				case StateStat.atk_gaia:
					cmd.Apply(ref atk.flat.gaia, ref atk.scale.gaia);
					break;

				case StateStat.atk_oida:
					cmd.Apply(ref atk.flat.oida, ref atk.scale.oida);
					break;

				case StateStat.atk_astra:
					cmd.Apply(ref atk.flat.astra, ref atk.scale.astra);
					break;

				case StateStat.atk_physical:
					cmd.Apply(ref atk.flat.physical, ref atk.scale.physical);
					break;

				case StateStat.atk_magical:
					cmd.Apply(ref atk.flat.magical, ref atk.scale.magical);
					break;

				// ----------------------------------------

				case StateStat.def:
					cmd.Apply(ref def.flat, ref def.scale);
					break;
				case StateStat.def_blunt:
					cmd.Apply(ref def.flat.blunt, ref def.scale.blunt);
					break;

				case StateStat.def_slash:
					cmd.Apply(ref def.flat.slash, ref def.scale.slash);
					break;

				case StateStat.def_pierce:
					cmd.Apply(ref def.flat.pierce, ref def.scale.pierce);
					break;

				case StateStat.def_gaia:
					cmd.Apply(ref def.flat.gaia, ref def.scale.gaia);
					break;

				case StateStat.def_oida:
					cmd.Apply(ref def.flat.oida, ref def.scale.oida);
					break;

				case StateStat.def_astra:
					cmd.Apply(ref def.flat.astra, ref def.scale.astra);
					break;

				case StateStat.def_physical:
					cmd.Apply(ref def.flat.physical, ref def.scale.physical);
					break;

				case StateStat.def_magical:
					cmd.Apply(ref def.flat.magical, ref def.scale.magical);
					break;


				// ----------------------------------------

				case StateStat.use_cost:
					cmd.Apply(ref usecost.flat, ref usecost.scale);
					break;

				case StateStat.skill_cost:
					cmd.Apply(ref skillcost.flat, ref skillcost.scale);
					break;

				case StateStat.sticker_cost:
					cmd.Apply(ref stickercost.flat, ref stickercost.scale);
					break;

				case StateStat.eflag:
					engineFlags.Add(cmd.flag);
					break;
			}

			cmds.Add(cmd);
		}

		public void Add(StateFunc func)
		{
			funcs.Add(func);
		}

		public void Add([NotNull] State state)
		{
			int idx = GetBuffIndex(state);
			if (idx > -1)
			{
				this.LogError($"{state} is already added to this BuffStats");
				return;
			}

			states.Add(state);
			entries.Add(new StateEntry(state, cmds.Count, funcs.Count));

			foreach (StateCmd cmd in state.status.cmds) Add(cmd);
			foreach (StateFunc fun in state.status.funcs) Add(fun);
		}

		public bool Remove([NotNull] State state)
		{
			if (entries.Count == 0)
				return false;

			if (!states.Remove(state))
				return false;

			const int SEARCHING  = 0;
			const int JUST_FOUND = 1;
			const int OFFSET     = 2;

			int search_state = SEARCHING;
			int find         = -1;
			int cmdshift     = 0;
			int funshift     = 0;
			for (var i = 0; i < entries.Count; i++)
			{
				StateEntry entry = entries[i];

				if (search_state == SEARCHING && entry.state == state)
				{
					find         = i;
					search_state = JUST_FOUND;
					continue;
				}

				if (search_state == JUST_FOUND)
				{
					StateEntry last = entries[i - 1];

					RemoveProps(last.cmdstart, entry.cmdstart);
					RemoveFuncs(last.funcstart, entry.funcstart);

					cmdshift = entry.cmdstart - last.cmdstart;
					funshift = entry.funcstart - last.funcstart;

					search_state = OFFSET;
				}

				if (search_state == OFFSET)
				{
					entry.funcstart -= funshift;
					entry.cmdstart  -= cmdshift;
				}

				entries[i] = entry;
			}

			if (search_state == SEARCHING)
			{
				this.LogError($"{state} is not in this buffstats.");
				return false;
			}

			if (search_state == JUST_FOUND)
			{
				// Erase to end
				StateEntry last = entries[entries.Count - 1];
				RemoveProps(last.cmdstart, cmds.Count);
				RemoveFuncs(last.funcstart, funcs.Count);
			}

			entries.RemoveAt(find);

			bool found = false;
			for (int i = 0; i < _lastRemovedUIDs.Count; i++)
			{
				RemovedState rs = _lastRemovedUIDs[i];
				if (rs.id == state.ID)
				{
					rs.uid              = state.UID;
					_lastRemovedUIDs[i] = rs;
					found               = true;
					break;
				}
			}

			if (!found)
			{
				_lastRemovedUIDs.Add(new RemovedState(state.ID, state.UID));
			}

			return true;
		}

		private void RemoveProps(int start, int end)
		{
			for (int i = end - 1; i >= start; i--)
			{
				RemoveCmd(i);
			}
		}

		private void RemoveFuncs(int start, int end)
		{
			for (int i = end - 1; i >= start; i--)
			{
				RemoveMod(i);
			}
		}

		private void RemoveCmd(int i)
		{
			StateCmd cmd = cmds[i];

			switch (cmd.stat)
			{
				case StateStat.points:
					cmd.Undo(ref points.flat, ref points.scale);
					break;

				case StateStat.hp:
					cmd.Undo(ref points.flat.hp, ref points.scale.hp);
					break;

				case StateStat.sp:
					cmd.Undo(ref points.flat.sp, ref points.scale.sp);
					break;

				case StateStat.op:
					cmd.Undo(ref points.flat.op, ref points.scale.op);
					break;

				// ----------------------------------------

				case StateStat.stats:
					cmd.Undo(ref stat.flat, ref stat.scale);
					break;

				case StateStat.power:
					cmd.Undo(ref stat.flat.power, ref stat.scale.power);
					break;

				case StateStat.speed:
					cmd.Undo(ref stat.flat.speed, ref stat.scale.speed);
					break;

				case StateStat.will:
					cmd.Undo(ref stat.flat.will, ref stat.scale.will);
					break;

				// ----------------------------------------

				case StateStat.res:
					cmd.Undo(ref resistance.flat, ref resistance.scale);
					break;

				case StateStat.res_blunt:
					cmd.Undo(ref resistance.flat.blunt, ref resistance.scale.blunt);
					break;

				case StateStat.res_slash:
					cmd.Undo(ref resistance.flat.slash, ref resistance.scale.slash);
					break;

				case StateStat.res_pierce:
					cmd.Undo(ref resistance.flat.pierce, ref resistance.scale.pierce);
					break;

				case StateStat.res_gaia:
					cmd.Undo(ref resistance.flat.gaia, ref resistance.scale.gaia);
					break;

				case StateStat.res_oida:
					cmd.Undo(ref resistance.flat.oida, ref resistance.scale.oida);
					break;

				case StateStat.res_astra:
					cmd.Undo(ref resistance.flat.astra, ref resistance.scale.astra);
					break;

				// ----------------------------------------

				case StateStat.atk:
					cmd.Undo(ref atk.flat, ref atk.scale);
					break;

				case StateStat.atk_blunt:
					cmd.Undo(ref atk.flat.blunt, ref atk.scale.blunt);
					break;

				case StateStat.atk_slash:
					cmd.Undo(ref atk.flat.slash, ref atk.scale.slash);
					break;

				case StateStat.atk_pierce:
					cmd.Undo(ref atk.flat.pierce, ref atk.scale.pierce);
					break;

				case StateStat.atk_gaia:
					cmd.Undo(ref atk.flat.gaia, ref atk.scale.gaia);
					break;

				case StateStat.atk_oida:
					cmd.Undo(ref atk.flat.oida, ref atk.scale.oida);
					break;

				case StateStat.atk_astra:
					cmd.Undo(ref atk.flat.astra, ref atk.scale.astra);
					break;

				// ----------------------------------------

				case StateStat.def:
					cmd.Undo(ref def.flat, ref def.scale);
					break;
				case StateStat.def_blunt:
					cmd.Undo(ref def.flat.blunt, ref def.scale.blunt);
					break;

				case StateStat.def_slash:
					cmd.Undo(ref def.flat.slash, ref def.scale.slash);
					break;

				case StateStat.def_pierce:
					cmd.Undo(ref def.flat.pierce, ref def.scale.pierce);
					break;

				case StateStat.def_gaia:
					cmd.Undo(ref def.flat.gaia, ref def.scale.gaia);
					break;

				case StateStat.def_oida:
					cmd.Undo(ref def.flat.oida, ref def.scale.oida);
					break;

				case StateStat.def_astra:
					cmd.Undo(ref def.flat.astra, ref def.scale.astra);
					break;

				// ----------------------------------------

				case StateStat.use_cost:
					cmd.Undo(ref usecost.flat, ref usecost.scale);
					break;

				case StateStat.skill_cost:
					cmd.Undo(ref skillcost.flat, ref skillcost.scale);
					break;

				case StateStat.sticker_cost:
					cmd.Undo(ref stickercost.flat, ref stickercost.scale);
					break;

				case StateStat.eflag:
					engineFlags.Remove(cmd.flag);
					break;
			}

			cmds.RemoveAt(i);
		}

		private void RemoveMod(int i)
		{
			funcs.RemoveAt(i);
		}

		private int GetBuffIndex(State state)
		{
			for (var i = 0; i < entries.Count; i++)
			{
				if (entries[i].state == state) return i;
			}

			return -1;
		}

		public void ModifyCost(UseInfo info, ref float cost)
		{
			foreach (StateFunc logic in funcs)
			{
				logic.ModifyCost(info, ref cost);
			}

			cost = cost.Mod(usecost);

			if (info.type == UseType.Skill) cost   = cost.Mod(skillcost);
			if (info.type == UseType.Sticker) cost = cost.Mod(stickercost);
		}

		public bool IsSkillForbidden(BattleSkill skill)
		{
			foreach (StateFunc logic in funcs)
			{
				if (logic.IsSkillForbidden(skill))
					return true;
			}

			return false;
		}

		public bool IsTargetForbidden(UseInfo info, Target target)
		{
			foreach (StateFunc logic in funcs)
			{
				if (logic.IsTargetForbidden(info, target))
					return true;
			}

			return false;
		}

		public void ModifyTargetOptions(Fighter user, UseInfo info, Targeting targeting)
		{
			foreach (StateFunc logic in funcs)
			{
				logic.ModifyTargetOptions(user, info, targeting);
			}
		}

		public void ModifyTargetPicks(Fighter user, UseInfo info, Targeting targeting)
		{
			foreach (StateFunc logic in funcs)
			{
				logic.ModifyTargetPicks(user, info, targeting);
			}
		}

		public BattleBrain ModifyBrain(BattleBrain original)
		{
			BattleBrain ret = original;
			foreach (StateFunc logic in funcs)
				ret = logic.ModifyBrain(original);

			return ret;
		}


		public struct StateEntry
		{
			public State state;
			public int   cmdstart;
			public int   funcstart;

			public StateEntry(State state, int cmdstart, int funcstart)
			{
				this.state     = state;
				this.cmdstart  = cmdstart;
				this.funcstart = funcstart;
			}
		}

		public struct RemovedState
		{
			public string id;
			public int    uid;

			public RemovedState(string id, int uid)
			{
				this.id  = id;
				this.uid = uid;
			}
		}
	}
}