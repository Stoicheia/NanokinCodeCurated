using Anjin.Nanokin.Map;
using Anjin.Scripting;
using Overworld.Cutscenes;

namespace Anjin.Scripting.Waitables {

	[LuaUserdata]
	public class ManagedDualStateComposite : CoroutineManaged {

		private DualStateComposite _composite;

		public override bool Active { get; }

		public ManagedDualStateComposite(DualStateComposite composite) {
			_composite = composite;
		}
	}
}