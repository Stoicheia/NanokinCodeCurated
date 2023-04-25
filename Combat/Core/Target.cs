using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Anjin.Scripting;
using Anjin.Util;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Pathfinding.Util;
using UnityEngine;
using Util;
using Vexe.Runtime.Extensions;

namespace Combat.Data
{
	/// <summary>
	/// Represents a selection of one or several targetables.
	/// A targetable is (so far) either a tile or a fighter.
	/// </summary>
	[LuaUserdata]
	public class Target : IEnumerable<ITargetable>
	{
		public static readonly Target Empty = new Target();

		public readonly List<ITargetable> all      = new List<ITargetable>();
		public readonly List<Fighter>     fighters = new List<Fighter>();
		public readonly List<Slot>        slots    = new List<Slot>();

		public bool showSlotReticle = false;

		public int index;

		/// <summary>
		/// Internal value that targeters can set to specify exactly where the center of the target is.
		/// This bypasses and overrides the VisualCentroid calculation.
		/// </summary>
		public Vector3? epicenter;

		private static List<object>    _tmpList       = new List<object>(4);
		private static List<SlotGrid> _scratchGroups = new List<SlotGrid>();

		public Target() { }

		public Target([NotNull] List<Fighter> fighters)
		{
			Add(fighters);
			RefreshInfo();
		}

		public Target([NotNull] List<Slot> slots)
		{
			Add(slots);
			RefreshInfo();
		}

		public Target([NotNull] List<ITargetable> targetables)
		{
			all.AddRange(targetables);
			RefreshInfo();
		}

		public Target(Fighter fighter)
		{
			Add(fighter);
			RefreshInfo();
		}

		public Target(Slot slot)
		{
			Add(slot);
			RefreshInfo();
		}

		public Target(ITargetable targetable)
		{
			Add(targetable);
			RefreshInfo();
		}

		public Target([NotNull] Table tbl)
		{
			Add(tbl);
			RefreshInfo();
		}

		public void Clear()
		{
			all.Clear();
			fighters.Clear();
			slots.Clear();
		}

		public void Add(ITargetable targetable)
		{
			all.Add(targetable);
		}

		private void Add(Fighter fighter)
		{
			fighters.Add(fighter);
			all.Add(fighter);
		}

		private void Add(Slot slot)
		{
			slots.Add(slot);
			all.Add(slot);
		}

		private void Add([NotNull] List<Fighter> fighters)
		{
			this.fighters.AddRange(fighters);
			all.AddRange(this.fighters);
		}

		private void Add([NotNull] List<Slot> slots)
		{
			this.slots.AddRange(slots);
			all.AddRange(this.slots);
		}


		public void Add([NotNull] Table table)
		{
			for (var i = 1; i <= table.Length; i++)
			{
				DynValue dv = table.Get(i);
				if (dv.AsObject(out Fighter fighter))
					Add(fighter);
				else if (dv.AsObject(out Slot slot))
					Add(slot);
			}
		}

		public int Count => all.Count;

		[NotNull]
		public string ToStringPretty()
		{
			StringBuilder sb = ObjectPoolSimple<StringBuilder>.Claim();
			sb.Append("(");
			sb.Append(fighters.JoinString());
			sb.Append(", ");
			sb.Append(slots.JoinString());
			sb.Append(")");

			var ret = sb.ToString();

			sb.Clear();
			ObjectPoolSimple<StringBuilder>.Release(ref sb);
			return ret;
		}

		public IEnumerator<ITargetable> GetEnumerator() => all.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)all).GetEnumerator();

		public override string ToString() => $"Target({ToStringPretty()})";

	#region API

		/// <summary>
		/// Centroid position of all targetables in this target.
		/// </summary>
		[UsedImplicitly]
		public Vector3 position { get; private set; }

		/// <summary>
		/// Centroid center of all targetables in this target.
		/// </summary>
		[UsedImplicitly]
		public Vector3 center { get; private set; }

		/// <summary>
		/// Centroid of targetables on the grid.
		/// </summary>
		[UsedImplicitly]
		public Vector2 gcenter { get; private set; }

		/// <summary>
		/// Center of all grids this targeting resides on.
		/// </summary>
		[UsedImplicitly]
		public Vector2 center_all_grids { get; private set; }

		/// <summary>
		/// Center of the first grid this targeting resides on.
		/// </summary>
		[UsedImplicitly]
		public Vector2 center_first_grid { get; private set; }

		/// <summary>
		/// Shortcut to get the first (or only) entity in this target.
		/// </summary>
		[UsedImplicitly]
		public Fighter Fighter => fighters[0];

		/// <summary>
		/// Shortcut to get the first (or only) slot in this target.
		/// </summary>
		[UsedImplicitly]
		public Slot Slot => slots[0];

		/// <summary>
		/// Shortcut to get the home of the first or only fighter in this target.
		/// </summary>
		[CanBeNull]
		[UsedImplicitly]
		public Slot Home => Fighter.home;

		[CanBeNull]
		[UsedImplicitly]
		public object First => this[0];

		[CanBeNull]
		[UsedImplicitly]
		public object At(int index) => this[index];

		[CanBeNull]
		[UsedImplicitly]
		public object Last => this[Count - 1];

		[UsedImplicitly]
		public bool IsEmpty => all.Count == 0;

		[UsedImplicitly]
		public bool IsSingle => all.Count == 1;

		[CanBeNull]
		[UsedImplicitly]
		public object this[int i] => all.SafeGet(i);

		/// <summary>
		/// Must be called before using the target
		/// in order to cache the centroids for use.
		/// </summary>
		public void RefreshInfo()
		{
			_scratchGroups.Clear();

			if (epicenter.HasValue)
			{
				center = epicenter.Value;
			}
			else
			{
				// Calculate position centroid
				var calc = new Centroid();
				foreach (ITargetable tfm in all)
					calc.add(tfm.GetTargetPosition());

				position = calc.get();

				// Calculate visual centroid
				calc = new Centroid();
				foreach (ITargetable tfm in all)
					calc.add(tfm.GetTargetCenter());

				center = calc.get();
			}

			// Calculate gcenter
			Centroid gcentroid = new Centroid();
			foreach (Fighter fighter in fighters)
			{
				if (fighter.home != null)
					gcentroid.add(fighter.home.coord.vec2());
			}

			foreach (Slot slot in slots)
			{
				gcentroid.add(slot.coord.vec2());

				if (slot.grid != null)
					_scratchGroups.AddIfNotExists(slot.grid);
			}

			if (slots.IsEmpty() && !fighters.IsEmpty())
			{
				foreach (Fighter fighter in fighters)
				{
					if (fighter.home != null && fighter.home.grid != null)
						_scratchGroups.AddIfNotExists(fighter.home.grid);
				}
			}

			gcenter = gcentroid.get();

			// Calculate grid centers
			if (_scratchGroups.IsEmpty())
			{
				center_all_grids  = Vector2.zero;
				center_first_grid = Vector2.zero;
			}
			else
			{
				center_first_grid = _scratchGroups[0].center_xz;
				Centroid all_grids = new Centroid();

				foreach (SlotGrid group in _scratchGroups)
					all_grids.add(group.center_xz);

				center_all_grids = all_grids.get();
			}
		}

		[UsedImplicitly]
		public int GetCount()
		{
			return Count;
		}


		/// <summary>
		/// Take the first N targets, discarding the rest.
		/// NOTE: do not keep the returned list! It is cached to reduce allocations.
		/// (however it's fine to save it in Lua scripts, since it's converted to a new table)
		/// </summary>
		[UsedImplicitly, NotNull]
		public List<object> Take(int n)
		{
			_tmpList.Clear();

			n = Mathf.Min(n, all.Count);
			for (var i = 0; i < n; i++)
			{
				_tmpList.Add(all[i]);
			}

			return _tmpList;
		}

		/// <summary>
		/// Skip the first N objects, taking only the rest.
		/// NOTE: do not keep the returned list! It is cached to reduce allocations.
		/// (however it's fine to save it in Lua scripts, since it's converted to a new table)
		/// </summary>
		[UsedImplicitly, NotNull]
		public List<object> Skip(int n)
		{
			_tmpList.Clear();

			for (var i = n; i < all.Count; i++)
			{
				_tmpList.Add(all[i]);
			}

			return _tmpList;
		}

	#endregion
	}
}