using System.Collections.Generic;
using Anjin.Util;
using JetBrains.Annotations;
using UnityEngine;
using Util;

namespace Combat.Data
{
	public class SlotGrid
	{
		public Battle     battle;
		public List<Slot> all;
		public SlotLayout component;
		public Team       team;

		/// <summary>
		/// The grid-coordinate center of the grid (i.e. slot (1,1), (4,1), etc.)
		/// </summary>
		public Vector2 center_coord;

		/// <summary>
		/// The centroid of all slots in world space
		/// </summary>
		public Vector2 center_xz;

		/// <summary>
		/// The centroid of all slots in world space
		/// </summary>
		public Vector2 center;

		private static readonly List<Slot> _tmpSlots = new List<Slot>();

		public SlotGrid(List<Slot> all)
		{
			this.all = all;
			OnSlotsChanged();
		}

		public SlotGrid(SlotLayout component)
		{
			all            = new List<Slot>();
			this.component = component;

			component.AddSlots(all);
			OnSlotsChanged();
		}

		private void OnSlotsChanged()
		{
			var ctr    = new Centroid();
			var ctr_xz = new Centroid();

			foreach (Slot slot in all)
			{
				ctr.add(slot.coord.vec2());
				ctr.add(slot.position);
				ctr_xz.add(slot.position.xz());
			}

			center_coord   = ctr;
			center    = ctr;
			center_xz = ctr_xz;
		}

		[CanBeNull]
		public Slot GetRandomFreeSlot()
		{
			_tmpSlots.Clear();
			_tmpSlots.AddRange(all);

			while (_tmpSlots.Count > 0)
			{
				int  idx  = RNG.Int(_tmpSlots.Count);
				Slot slot = _tmpSlots[idx];

				if (!slot.taken)
					return slot;

				_tmpSlots.RemoveAt(idx);
			}

			return null;
		}

		public List<Slot> RandomSlots(List<Slot> allslots, int count)
		{
			List<Slot> choices = new List<Slot>();

			_tmpSlots.Clear();
			_tmpSlots.AddRange(allslots);

			for (var i = 0; i < count; i++)
			{
				int sel = RNG.Int(_tmpSlots.Count);
				choices.Add(_tmpSlots[sel]);
				_tmpSlots.RemoveAt(sel);
			}

			return choices;
		}

		public Slot GetSlotAt(Vector2Int coord) => component.GetSlotAt(this, coord);

		public Slot GetDefaultSlot(int i) => component.GetDefaultSlot(this, i);
	}
}