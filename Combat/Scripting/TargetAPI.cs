using System.Collections.Generic;
using Anjin.Scripting;
using Combat.Data;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Combat
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	[LuaUserdata("TargetAPI")]
	public class TargetAPI
	{
		public const int MAX_SHAPE_SIZE = 4;

		private static bool[,]    _tempShape = new bool[MAX_SHAPE_SIZE, MAX_SHAPE_SIZE];
		private static List<Slot> _tempslots = new List<Slot>();

		public static Target new_target(Table tbl)
		{
			return new Target(tbl);
		}

		public static Target new_target(Fighter fter)
		{
			return new Target(fter);
		}

		public static Target new_target(Slot slot)
		{
			return new Target(slot);
		}

		/// <summary>
		/// Return targets of slots for a shape that is fitted onto the field.
		/// The shape string should look something like this:
		///
		/// ooo
		/// o o
		/// ooo
		///
		/// - Spaces for holes.
		/// - Any character for a filled slot.
		///
		/// NOTE:
		///
		/// The first line can be used to set some flags.
		///
		/// - p: Enable partial targeting
		/// - x: Enable crossteam targeting
		///
		/// E.g.:
		///
		/// @"px
		/// ooo
		/// o o
		/// ooo
		/// "
		///
		/// or in Lua:
		///
		/// target = [[px
		/// ooo
		/// o o
		/// ooo
		/// ]]
		///
		/// </summary>
		public static List<Target> shape(
			Battle         battle,
			[CanBeNull] Fighter user,
			[NotNull]   string  shapeDefinition,
			bool                allowPartial    = false,
			bool                allowCrossteam  = false,
			List<Slot>          set             = null,
			bool                enableTextFlags = true

		)
		{
			List<Target> ret = new List<Target>();

			// Read shape from the string, e.g.:
			//
			//  o
			// ooo
			//  o
			//
			// ----------------------------------------

			int  w       = 0;
			int  h       = 0;
			bool shaping = false;

			shapeDefinition = shapeDefinition.Replace("\n\n", "\n");

			for (int i = 0, x = 0, y = 0; i < shapeDefinition.Length; i++)
			{
				char c = shapeDefinition[i];
				if (c == '\t') continue;
				if (c == '\r') continue;

				// Start by looking for flags
				if (!shaping && enableTextFlags)
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

				_tempShape[x, y] = c == 'o' || c == 'x';
				shaping          = true;

				x++;
				w = Mathf.Max(w, x);
			}

			// Test the shape against each slot
			// ----------------------------------------
			set = set ?? battle.slots;

			foreach (Slot center in set)
			{
				// if (filter != null)
				// {
				// 	var dvOk = Lua.Invoke(filter, LuaUtil.Args(center));
				// 	if (dvOk.Type == DataType.Boolean && dvOk.Boolean == false)
				// 	{
				// 		continue;
				// 	}
				// }

				int xc = center.coord.x;           // center
				int yc = center.coord.y;           // center
				int xo = Mathf.FloorToInt(w / 2f); // offset
				int yo = Mathf.FloorToInt(h / 2f); // offset

				_tempslots.Clear();

				for (var i = 0; i < w; i++)
				for (var j = 0; j < h; j++)
				{
					bool b       = _tempShape[i, j];
					Slot overlap = battle.GetSlot(xc + i - xo, yc + j - yo);

					if (overlap == null)
					{
						if (!allowPartial) goto skip_permutation; // goto cringe!!! java better!!
						continue;
					}

					if (!allowCrossteam && overlap.team != center.team)
					{
						if (!allowPartial) goto skip_permutation;
						continue;
					}

					if (!set.Contains(overlap))
					{
						if (!allowPartial) goto skip_permutation;
						continue;
					}

					if (b)
					{
						_tempslots.Add(overlap);
					}
				}

				if (_tempslots.Count > 0)
				{
					Target t = new Target(_tempslots);
					//t.showReticle = false;
					// {
					// epicenter = center.position
					// };

					ret.Add(t);
				}

				skip_permutation:
				// ReSharper disable once RedundantJumpStatement
				continue;
			}

			// Reset stateers
			_tempslots.Clear();

			for (var i = 0; i < MAX_SHAPE_SIZE; i++)
			for (var j = 0; j < MAX_SHAPE_SIZE; j++)
			{
				_tempShape[i, j] = false;
			}


			return ret;
		}
	}
}