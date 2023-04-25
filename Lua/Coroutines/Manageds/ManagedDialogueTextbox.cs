using Anjin.Scripting.Waitables;
using Overworld.Cutscenes;

namespace Anjin.Scripting.Waitables {

	[LuaUserdata]
	public class ManagedActivatableWithTransitions : CoroutineManaged {

		public IActivatableWithTransitions Activatable;
		public ManagedActivatableWithTransitions(IActivatableWithTransitions activatable) => Activatable = activatable;
		public override bool Active => Activatable != null && Activatable.IsActive;

		public override void OnEnd(bool forceStopped, bool skipped = false)
		{
			if(!skipped)
				Activatable.Hide();
			else
				Activatable.HideInstant();
		}
	}
}