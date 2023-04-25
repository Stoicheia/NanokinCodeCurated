using System.Collections.Generic;
using Anjin.Scripting;
using JetBrains.Annotations;
using MoonSharp.Interpreter;

namespace Anjin.Utils
{
	/// <summary>
	/// A path for the MotionBehaviour to follow.
	/// </summary>
	[LuaUserdata]
	public class MotionPath
	{
		public List<MotionPoint> waypoints = new List<MotionPoint>();
		public MotionDef?        defaultDef;

		[NotNull]
		public static MotionPath FromTB([NotNull] Table tbl)
		{
			var             motion     = new MotionPath();
			List<MotionDef> motionDefs = new List<MotionDef>();

			for (var i = 1; i <= tbl.Length; i++)
			{
				DynValue dv = tbl.Get(i);

				if (dv.as_worldpoint2(out WorldPoint2 mtarget)) // waypoint
				{
					motion.AddWaypoint(mtarget);
				}
				else if (dv.AsObject(out MotionDef def))
				{
					motion.defaultDef = def;
					motionDefs.Add(def);
				}
			}

			for (var i = 0; i < motionDefs.Count && i < motion.waypoints.Count - 1; i++)
			{
				motion.waypoints[i + 1] = new MotionPoint
				{
					target = motion.waypoints[i + 1].target,
					motion = motionDefs[i]
				};
			}

			return motion;
		}

		public void AddWaypoint(WorldPoint2 target)
		{
			waypoints.Add(new MotionPoint
			{
				target = target,
				motion = null
			});
		}

		public void AddWaypoint(MotionPoint wp)
		{
			waypoints.Add(wp);
		}

		public void Init()
		{
			for (var i = 0; i < waypoints.Count; i++)
			{
				MotionPoint wp     = waypoints[i];
				WorldPoint2 target = wp.target;

				wp.target    = target;
				waypoints[i] = wp;
			}
		}
	}
}