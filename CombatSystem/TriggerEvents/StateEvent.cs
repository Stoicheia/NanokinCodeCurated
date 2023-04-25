using JetBrains.Annotations;
using MoonSharp.Interpreter;

namespace Combat.Data
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class StateEvent : TriggerEvent
	{
		private State _state;

		public StateEvent(State state)
		{
			_state = state;
			noun   = state;
		}

		public bool has_tag(string t1 = null, string t2 = null, string t3 = null, string t4 = null, string t5 = null, string t6 = null) => _state.has_tag(t1, t2, t3, t4, t5, t6);

		public bool has_tags(string t1 = null, string t2 = null, string t3 = null, string t4 = null, string t5 = null, string t6 = null) => _state.has_tag(t1, t2, t3, t4, t5, t6);

		public bool has_tag([NotNull] Table tbl) => _state.has_tag(tbl);

		public bool has_tags([NotNull] Table tbl) => _state.has_tags(tbl);
	}
}