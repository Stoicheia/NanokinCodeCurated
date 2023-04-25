using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Scripting;
using Anjin.Util;
using Combat.Features.TurnOrder.Sampling.Operations;
using Combat.Proxies;
using Combat.Toolkit;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using Pathfinding.Util;
using UnityEngine;
using Util;
using Util.Extensions;

namespace Combat.Data
{
	// public static class ProcHelpers
	// {
	// 	public static void Up([NotNull] this        ProcEffect eff, Table tbl) { eff.XAlter(tbl, Trigger.XAlters.Up); }
	// 	public static void Down([NotNull] this      ProcEffect eff, Table tbl) { eff.XAlter(tbl, Trigger.XAlters.Down); }
	// 	public static void Set([NotNull] this       ProcEffect eff, Table tbl) { eff.XAlter(tbl, Trigger.XAlters.Set); }
	// 	public static void Scale([NotNull] this     ProcEffect eff, Table tbl) { eff.XAlter(tbl, Trigger.XAlters.Scale); }
	// 	public static void ScaleUp([NotNull] this   ProcEffect eff, Table tbl) { eff.XAlter(tbl, Trigger.XAlters.ScaleUp); }
	// 	public static void ScaleDown([NotNull] this ProcEffect eff, Table tbl) { eff.XAlter(tbl, Trigger.XAlters.ScaleDown); }
	// }

	public enum ProcKinds
	{
		Fighter,
		Slot,
		Default
	}

	/// <summary>
	/// A proc is a one-shot batched execution of some battle effects
	/// onto a list of victims.
	/// Proc effects can be evaluated to expand into other effects.
	/// </summary>
	public class Proc : BattleResource
	{
		/// <summary>
		/// ID of the proc.
		/// </summary>
		[CanBeNull]
		public List<string> tags;

		/// <summary>
		/// The fighter which is dealing this proc.
		/// </summary>
		[CanBeNull]
		public Fighter dealer;

		/// <summary>
		/// The victims of the proc.
		/// </summary>
		public readonly List<Fighter> fighters = new List<Fighter>();

		/// <summary>
		/// The slot victims of the proc.
		/// </summary>
		public readonly List<Slot> slots = new List<Slot>();

		/// <summary>
		/// Effects of the proc.
		/// </summary>
		public List<ProcEffect> effects = new List<ProcEffect>();

		/// <summary>
		/// We apply effects to slots instead of fighters.
		/// We also reverse the auto-slot logic.
		/// i.e. by default we automatically fetch the fighters on the slots,
		/// now instead we will get the slots that the fighters are on.
		/// </summary>
		public ProcKinds kind = ProcKinds.Fighter;

		/// <summary>
		/// Disable the damage numbers for this proc.
		/// </summary>
		public bool noNumbers = false;

		/// <summary>
		/// Whether or not the proc has fired.
		/// Each proc should only fire once, so this is a safety check.
		/// </summary>
		public bool fired = false;

		public Action<ProcContext, Coplayer, ProcAnimation> onAnimating;

		// public List<object> Victims = new List<object>(); // TODO this isn't right at all

		public WorldPoint pos
		{
			get
			{
				var c = new Centroid();

				List<object> list = ListPool<object>.Claim();
				GetVictims(list);
				foreach (object v in list)
				{
					switch (v)
					{
						case Fighter f:
							c.add(f.position);
							break;

						case Slot s:
							c.add(s.position);
							break;
					}
				}

				ListPool<object>.Release(list);

				return c.get();
			}
		}

		public WorldPoint center
		{
			get
			{
				var c = new Centroid();

				List<object> list = ListPool<object>.Claim();
				GetVictims(list);
				foreach (object v in list)
				{
					switch (v)
					{
						case Fighter f:
							c.add(f.Center);
							break;

						case Slot s:
							c.add(s.Center);
							break;
					}
				}

				ListPool<object>.Release(list);

				return c.get();
			}
		}

		/// <summary>
		/// Get actor of the first victim.
		/// </summary>
		[UsedImplicitly]
		public ActorBase actor
		{
			get
			{
				List<object> list = ListPool<object>.Claim();
				GetVictims(list);
				foreach (object v in list)
				{
					switch (v)
					{
						case Fighter f:
							ListPool<object>.Release(list);
							return f.actor;

						case Slot s:
							ListPool<object>.Release(list);
							return s.actor;
					}
				}

				return null;
			}
		}

		/// <summary>
		/// Get transform of the first victim.
		/// </summary>
		[UsedImplicitly]
		public Transform transform
		{
			get
			{
				List<object> list = ListPool<object>.Claim();
				GetVictims(list);
				foreach (object v in list)
				{
					switch (v)
					{
						case Fighter f:
							ListPool<object>.Release(list);
							return f.actor.transform;

						case Slot s:
							ListPool<object>.Release(list);
							return s.actor.transform;
					}
				}

				return null;
			}
		}

		/// <summary>
		/// Get the facing of the first victim's actor.
		/// </summary>
		[UsedImplicitly]
		public Vector3 facing
		{
			get
			{
				List<object> list = ListPool<object>.Claim();
				GetVictims(list);
				foreach (object v in list)
				{
					switch (v)
					{
						case Fighter f:
							ListPool<object>.Release(list);
							return f.actor.facing;

						case Slot s:
							ListPool<object>.Release(list);
							return s.actor.facing;
					}
				}

				return Vector3.zero;
			}
		}

		public Proc(Battle battle)
		{
			this.battle = battle;
		}

		public override void AddTarget(Slot slot)
		{
			AddVictim(slot);
		}

		public override void AddTarget(Fighter fighter)
		{
			AddVictim(fighter);
		}

		public void AddEffect(ProcEffect effect)
		{
			if (effect == null) return;
			effects.Add(effect);
			effect.Parent = this;
		}

		public void AddEffects([CanBeNull] List<ProcEffect> effs)
		{
			if (effs == null) return;
			for (var i = 0; i < effs.Count; i++)
			{
				AddEffect(effs[i]);
			}
		}

		public void AddEffect([NotNull] State state)
		{
			AddEffect(new AddState(state));
			state.Parent = this;
		}

		public void AddEffect([NotNull] Fighter fter)
		{
			AddEffect(new AddFighter(fter));
			fter.Parent = this;
		}

		public void RemoveVictim(Fighter fighter)
		{
			if (!fighters.Remove(fighter))
			{
				DebugLogger.LogWarning($"Fighter {fighter.info.Name} not found.", LogContext.Combat);
			}
		}

		public void AddVictim(Fighter fighter)
		{
			hasSpecifiedTargets = true;

			if (fighter == null) return;
			if (fighter.dead) return;
			fighters.Add(fighter);
		}

		public void AddVictims([NotNull] List<Fighter> fighters)
		{
			foreach (Fighter fighter in fighters)
				AddVictim(fighter);
		}

		public void AddVictim(Slot slot)
		{
			hasSpecifiedTargets = true;
			if (slot == null) return;
			slots.Add(slot);
		}

		public void AddVictims([NotNull] List<Slot> slots)
		{
			foreach (Slot slot in slots)
				AddVictim(slot);
		}

		public void AddVictims([NotNull] List<IStatee> statees)
		{
			foreach (IStatee statee in statees)
			{
				switch (statee)
				{
					case Fighter fter:
						AddVictim(fter);
						break;

					case Slot slot:
						AddVictim(slot);
						break;
				}
			}
		}

		public override string ToString() => $"Proc-<{ID ?? ""}>-{{{(dealer == null ? "" : $"dealer={dealer}, ")}victims={fighters.JoinString()}, fx={effects.JoinString()}}}";

		public override void ConfigureTB([CanBeNull] Table tb)
		{
			if (tb == null) return;
			ID        = tb.TryGet("id", ID);
			dealer    = tb.TryGet("dealer", dealer);
			noNumbers = tb.TryGet("no_numbers", noNumbers);

			if (tb.TryGet("slot", out bool slotKind) && slotKind)
				kind = ProcKinds.Slot;

			if (tb.ContainsKey("victim")) AddVictimsDV(tb.Get("victim"));
			if (tb.ContainsKey("victims")) AddVictimsDV(tb.Get("victims"));
			if (tb.ContainsKey("onto")) AddVictimsDV(tb.Get("onto"));

			for (var i = 1; i <= tb.Length; i++)
				ConfigureDV(tb.Get(i));
		}

		public void ConfigureDV([NotNull] DynValue dv)
		{
			if (dv.IsNil())
			{
				this.LogError("Configure(dv) argument is nil.");
				return;
			}
			else if (dv.AsString(out string iget)) ConfigureDV(GetEnv().iget(iget));
			else if (dv.AsObject(out Table tb)) ConfigureTB(tb);
			else if (dv.AsExact(out Target target))
			{
				foreach (Fighter ft in target.fighters)
					AddVictim(ft);

				foreach (Slot sl in target.slots)
					if (sl.owner != null)
						AddVictim(sl);
			}
			else if (dv.AsObject(out State state)) AddEffect(state);
			// else if (dv.AsExact(out Slot slot)) AddVictim(slot);
			else if (dv.AsExact(out Fighter fighter)) AddEffect(fighter);
			else if (dv.AsExact(out ProcEffect eff)) effects.Add(eff);
			else if (dv.AsExact(out List<Fighter> fighters)) AddVictims(fighters);
			else if (dv.AsExact(out List<ProcEffect> effects)) AddEffects(effects);
			else if (dv.AsExact(out TurnFunc turnopfunc)) AddEffect(new TurnFuncEffect(turnopfunc));
			else
			{
				this.LogError($"Configure(dv) argument is not a valid type: {dv.Type},{dv.UserData.Object?.GetType().Name}");
				return;
			}
		}

		private void AddVictimsDV(DynValue dvictims)
		{
			hasSpecifiedTargets = true;
			if (dvictims.AsObject(out Fighter ft)) AddVictim(ft);
			else if (dvictims.AsObject(out List<Fighter> fts)) AddVictims(fts);
		}

		public bool GetVictims(List<object> victims)
		{
			switch (kind)
			{
				case ProcKinds.Fighter:
					foreach (Fighter fter in fighters)
						victims.Add(fter);

					foreach (Slot slot in slots)
					{
						if (slot.owner != null)
							victims.Add(slot.owner);
						else
							victims.Add(slot);
					}

					if (victims.Count == 0)
					{
						this.LogWarn($"{this} has no victims to apply on!");
						return true;
					}

					break;

				case ProcKinds.Slot:
					foreach (Slot slot in slots) victims.Add(slot);
					foreach (Fighter fter in fighters)
						if (fter.home != null)
							victims.Add(fter);
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}

			return false;
		}

	#region Offset Positioning Functions

		[UsedImplicitly]
		public WorldPoint anchor(string id) => actor.anchor(id);

		[UsedImplicitly]
		public WorldPoint rel_offset(Vector3 from, float fwd, float y, float horizontal) => actor.rel_offset(from, fwd, y, horizontal);

		[UsedImplicitly]
		public WorldPoint xy_offset(float fwd, float y, float horizontal = 0) => actor.xy_offset(fwd, y, horizontal);

		[UsedImplicitly]
		public WorldPoint offset(float z, float y, float x = 0) => actor.offset(z, y, x);

		[UsedImplicitly]
		public Vector3 offset3(float z, float y, float x)
		{
			// Technically this is the same as offset_zyx right above
			// We should try to remove offset3 and replace with simply offset to remain consistent
			return actor.center + facing * z + actor.Up * y + Vector3.Cross(facing, Vector3.up) * x;
		}

		[UsedImplicitly]
		public WorldPoint identity_offset(float d) => actor.identity_offset(d);

		[UsedImplicitly]
		public WorldPoint polar_offset(float rad, float angle, float horizontal = 0) => actor.polar_offset(rad, angle, horizontal);

		[UsedImplicitly]
		public WorldPoint ahead(float distance, float horizontal = 0) => actor.ahead(distance, horizontal);

		[UsedImplicitly]
		public WorldPoint behind(float distance, float horizontal = 0) => actor.behind(distance, horizontal);

		[UsedImplicitly]
		public WorldPoint above(float distance = 0) => actor.above(distance);

		[UsedImplicitly]
		public WorldPoint under(float distance) => actor.under(distance);

	#endregion
	}


	public enum ProcEffectFlags
	{
		NoEffect,
		VictimEffect,
		DecorativeEffect,
		MetaEffect
	}

	public struct ProcAnimation
	{
		public string  id;
		public Type    type;
		public Closure closure;

		public enum Type
		{
			Proc,
			Victim,
			Swapee
		}
	}
}