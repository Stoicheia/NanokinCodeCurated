using System.Collections.Generic;
using Anjin.Scripting;
using JetBrains.Annotations;
using UnityEngine;

// ReSharper disable UnusedMember.Global

// ReSharper disable PossibleNullReferenceException

namespace Combat.UI
{
	public enum TargetUITeam
	{
		Opponent,
		Player
	}

	[LuaEnum]
	public enum TargetUIIconType
	{
		None,

		/// <summary>
		/// Epicenter of the target (the user)
		/// </summary>
		Epicenter,

		/// <summary>
		/// Free target, unlocked from the grid.
		/// </summary>
		Free,

		Move1,

		Move2,

		Teleport,

		Indicator
	}

	public enum TargetUIHighlight
	{
		Empty,
		Half,
		Full
	}

	public struct TargetUISlot
	{
		public TargetUIHighlight highlight;
		public TargetUITeam      team;
		public TargetUIIconType  IconType;
	}

	public struct TargetUIIcon
	{
		public int              x;
		public int              y;
		public TargetUIIconType type;
		public float            rot;
		public string			indicator;
	}

	public class TargetUIGrid
	{
		public Vector2Int size;
		public Vector2Int offset;

		public TargetUITeam       team;
		public TargetUISlot[,]    slots;
		public List<TargetUIIcon> icons;

		public TargetUIGrid()
		{
			team  = team;
			icons = new List<TargetUIIcon>();
		}

		public void InitGrid(int w, int h)
		{
			size  = new Vector2Int(w, h);
			slots = new TargetUISlot[w, h];
		}
	}

	[LuaUserdata]
	public class TargetUILua
	{
		[CanBeNull] public string _description;

		public TargetUIGrid Grid = new TargetUIGrid();

		// NOTE: Negative coords are NOT SUPPORTED
		private Vector2Int? _bounds;

		public Vector2Int Bounds
		{
			get
			{
				if (_bounds == null)
				{
					var v = Grid.size + Grid.offset;

					foreach (TargetUIIcon icon in Grid.icons)
					{
						if (icon.x > v.x) v = new Vector2Int(icon.x + 1, v.y);
						if (icon.y > v.y) v = new Vector2Int(v.x, icon.y + 1);
					}

					_bounds = v;
				}

				return _bounds.Value;
			}
		}

		public TargetUILua() { _description = null; }

		private void InsureGrid()
		{
			if (Grid.slots == null) full_grid();
		}


		// API

		public void grid(int w, int h)
		{
			_bounds = null;
			Grid.InitGrid(w, h);
		}

		public void full_grid()
		{
			grid(3, 3);
		}

		public void grid_offset(int x, int y)
		{
			InsureGrid();
			Grid.offset = new Vector2Int(x, y);
			_bounds     = null;
		}

		public void semihighlight(int x, int y)
		{
			InsureGrid();
			if (x < 0 || y < 0 || x > Grid.size.x - 1 || y > Grid.size.y - 1) return;
			Grid.slots[x, y].highlight = TargetUIHighlight.Half;
		}

		public void highlight(int x, int y)
		{
			InsureGrid();
			if (x < 0 || y < 0 || x > Grid.size.x - 1 || y > Grid.size.y - 1) return;
			Grid.slots[x, y].highlight = TargetUIHighlight.Full;
		}

		public void description(string decs) => _description = decs;


		public void team_player() => team(TargetUITeam.Player);

		public void team(TargetUITeam team)
		{
			InsureGrid();
			for (int y = 0; y < Grid.size.y; y++)
			{
				for (int x = 0; x < Grid.size.x; x++)
				{
					Grid.slots[x, y].team = team;
				}
			}
		}

		private static bool[,] _tempShape = new bool[TargetAPI.MAX_SHAPE_SIZE, TargetAPI.MAX_SHAPE_SIZE];

		public void do_targeting_string(string shapeDefinition)
		{
			int  w       = 0;
			int  h       = 0;
			bool shaping = false;

			bool allowPartial   = false;
			bool allowCrossteam = false;

			for (int i = 0, x = 0, y = 0; i < shapeDefinition.Length; i++)
			{
				char c = shapeDefinition[i];
				if (c == '\t') continue;

				// Start by looking for flags
				if (!shaping)
				{
					if (c == 'p')
					{
						allowPartial = true;
						continue;
					}

					if (c == 'x')
					{
						allowCrossteam = true;
						continue;
					}
				}

				if (c == '\n')
				{
					if (shaping)
					{
						y++;
						h++;
					}

					x = 0;
					continue;
				}

				_tempShape[x, y] = c == 'o';
				shaping          = true;

				x++;
				w = Mathf.Max(w, x);
			}

			grid(w, h);

			for (int y = 0; y < h; y++)
			{
				for (int x = 0; x < w; x++)
				{
					if (_tempShape[x, y])
					{
						highlight(x, y);
					}
				}
			}
		}

		public void icon_epicenter(int x, int y, float rot = 0) => icons(x, y, TargetUIIconType.Epicenter, rot);
		public void icon_free(int      x, int y, float rot = 0) => icons(x, y, TargetUIIconType.Free, rot);
		public void icon_move1(int     x, int y, float rot = 0) => icons(x, y, TargetUIIconType.Move1, rot);
		public void icon_move2(int     x, int y, float rot = 0) => icons(x, y, TargetUIIconType.Move2, rot);
		public void icon_teleport(int  x, int y, float rot = 0) => icons(x, y, TargetUIIconType.Teleport, rot);
		public void icon_indicator(int x, int y, string indicator, float rot = 0) => icons(x, y, TargetUIIconType.Indicator, rot, indicator);

		public void icons(int x, int y, TargetUIIconType iconType, float rot = 0, string indicator = "")
		{
			Grid.icons.Add(new TargetUIIcon { x = x, y = y, type = iconType, rot = rot, indicator = indicator });
			_bounds = null;
		}
	}
}