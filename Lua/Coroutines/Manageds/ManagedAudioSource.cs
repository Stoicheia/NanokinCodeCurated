using Overworld.Cutscenes;
using UnityEngine;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class ManagedAudioSource : CoroutineManaged
	{
		private readonly AudioSource _src;

		public ManagedAudioSource(AudioSource src)
		{
			_src = src;
		}

		public override bool Active => _src.isPlaying;

		public override void OnEnd(bool forceStopped, bool skipped = false)
		{
			_src.Stop();
		}
	}
}