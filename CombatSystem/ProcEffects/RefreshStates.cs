using System.Collections.Generic;

namespace Combat.Data
{
	public class RefreshStates : ProcEffect
	{
		public string key;

		public RefreshStates(string key)
		{
			this.key = key;
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			IEnumerable<State> matchedBuffs = battle.GetStatesByID(key);

			foreach (State mstate in matchedBuffs)
			{
				mstate.Refresh();
			}

			return ProcEffectFlags.VictimEffect;
		}
	}
}