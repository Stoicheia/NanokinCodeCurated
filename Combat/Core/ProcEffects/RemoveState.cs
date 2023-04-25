using MoonSharp.Interpreter;

namespace Combat.Data
{
	public class RemoveState : ProcEffect
	{
		private string _id;
		private int    _count = -1;
		private State   _state;
		private Table  _filter;

		public RemoveState(State state)
		{
			_state = state;
		}

		public RemoveState(string id, int count = -1)
		{
			_id    = id;
			_count = count;
		}

		public RemoveState(Table filter, int count = -1)
		{
			_filter = filter;
			_count  = count;
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			if (_state != null) battle.LoseState(fighter, _state);
			if (_id != null) battle.LoseStates(fighter, _id, count: _count);
			if (_filter != null) battle.LoseStates(fighter, _filter, _count);

			return ProcEffectFlags.VictimEffect;
		}
	}
}