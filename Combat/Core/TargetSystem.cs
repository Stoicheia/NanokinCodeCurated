using System.Collections.Generic;
using Anjin.Scripting;

namespace Combat.Data
{
	[LuaUserdata(Descendants = true)]
	public abstract class TargetSystem
	{
		public Friendliness friendRestriction;

		public TargetSystem() { }

		protected TargetSystem(Friendliness friendRestriction)
		{
			this.friendRestriction = friendRestriction;
		}

		public abstract List<Target> EvaluateTargets(Battle battle, Fighter source);
	}
}