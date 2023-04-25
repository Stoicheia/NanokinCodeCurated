using Anjin.Utils;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class WaitableFreezeFrame : ICoroutineWaitable
	{
		private readonly FreezeFrameVolume _volume;

		private bool _ended;

		public WaitableFreezeFrame(FreezeFrameVolume volume)
		{
			_volume         =  volume;
			_volume.onEnded += () => _ended = true;
		}

		public bool CanContinue(bool justYielded, bool isCatchup) => _ended;
	}
}