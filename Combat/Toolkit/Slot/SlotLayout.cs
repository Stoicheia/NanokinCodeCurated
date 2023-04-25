using System;
using System.Collections.Generic;
using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Math.Splines;

namespace Combat.Data
{
	public class SlotLayout : MonoBehaviour
	{
		public enum Types
		{
			Single,
			Grid,
			List
		}


		[Serializable]
		public struct SlotInfo
		{
			public Vector2Int Coordinate;
			public Vector2Int Forward;
			public string[]   Tags;
		}

		[Serializable]
		public struct ColumnInfo
		{
			public SlotLine   Line;
			public Vector2Int Forward;
			public string[]   Tags;
		}

		[Serializable]
		public struct GridInfo
		{
			public GridShape  Line;
			public Vector2Int Forward;
			public string[]   Tags;
		}

		public Types Type;

		public Vector2Int Coordinate = Vector2Int.zero;
		public Vector2Int Forward    = Vector2Int.zero;

		[ShowIf("@Type == SlotLayout.Types.Grid")]
		public Vector2Int GridDimensions;
		public List<ColumnInfo> GridColumns;

		[ShowIf("@Type == SlotLayout.Types.List")]
		public List<SlotInfo> List = new List<SlotInfo>();

		[Required] public PlotShape CoachShape;
		[Required] public PlotShape VictoryShape;
		[Required] public PlotShape SpawnShape;

		public void AddSlots(List<Slot> slots)
		{
			switch (Type)
			{
				case Types.Single:
					break;

				case Types.Grid:
					for (var x = 0; x < GridDimensions.x; x++)
					for (var y = 0; y < GridDimensions.y; y++)
					{
						var slot = new Slot
						{
							coord = Coordinate + new Vector2Int(x, y)
						};

						if (GridColumns.Count > 0)
						{
							ColumnInfo column = GridColumns[x];
							slot.forward = column.Forward;
							slot.tags.AddRange(column.Tags);

							Plot plot = column.Line.Get(y, GridDimensions.y);
							slot.position = plot.position;
							slot.facing   = plot.facing;
						}
						else
						{
							// TODO
						}

						slots.Add(slot);
					}


					break;

				case Types.List:
					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public Slot GetSlotAt(SlotGrid grid, Vector2Int coord)
		{
			return grid.battle.GetSlot(Coordinate + coord);
		}

		public virtual Slot GetDefaultSlot(SlotGrid grid, int index)
		{
			switch (Type)
			{
				case Types.Single:
					return grid.all[0];

				case Types.Grid:
					Slot slot = GetDefaultGridSLot(grid, index);

					// Choose a random slot
					for (int i = 0; (slot == null || slot.taken) && i < grid.all.Count; i++)
					{
						Vector2Int pos = Coordinate + new Vector2Int(RNG.Range(0, 2), RNG.Range(0, 2));
						slot = grid.battle.GetSlot(pos);
					}

					if (slot?.taken == true)
						slot = null;

					return slot;

				case Types.List:
					return grid.all.SafeGet(index);

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		private Slot GetDefaultGridSLot(SlotGrid grid, int index)
		{
			switch (index)
			{
				case 0: return grid.battle.GetSlot(Coordinate + new Vector2Int(1, 1)); // center of 3x3
				case 1: return grid.battle.GetSlot(Coordinate + new Vector2Int(1, 0)); // top of center col
				case 2: return grid.battle.GetSlot(Coordinate + new Vector2Int(1, 1)); // bottom of back col
				case 3: return grid.battle.GetSlot(Coordinate + new Vector2Int(0, 1)); // center of back col
				default:
					return null;
			}
		}
	}
}