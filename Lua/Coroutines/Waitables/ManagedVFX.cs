using Combat.Data.VFXs;
using Overworld.Cutscenes;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class ManagedVFX : CoroutineManaged
	{
		private readonly VFX _vfx;

		public ManagedVFX(VFX vfx)
		{
			_vfx = vfx;
		}

		public override bool Active => _vfx.IsActive;

		public override void OnEnd(bool forceStopped, bool skipped = false)
		{
			_vfx.EndPrematurely();
		}

	}
}