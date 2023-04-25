using Anjin.Scripting;
using JetBrains.Annotations;

namespace Combat.Data
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class ProcStatsProxy : LuaProxy<ProcStatus>
	{
		public void set(ProcStat stat, float value)
		{
			proxy.set(stat, value);
		}

		public void up(ProcStat stat, float value)
		{
			proxy.up(stat, value);
		}

		public void down(ProcStat stat, float value)
		{
			proxy.down(stat, value);
		}

		public void scale(ProcStat stat, float value)
		{
			proxy.scale(stat, value);
		}
	}
}