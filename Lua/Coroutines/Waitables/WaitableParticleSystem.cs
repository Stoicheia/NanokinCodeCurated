using UnityEngine;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class WaitableParticleSystem : ICoroutineWaitable
	{
		private readonly ParticleSystem _psystem;

		public WaitableParticleSystem(ParticleSystem psystem)
		{
			_psystem = psystem;
		}

		public virtual bool CanContinue(bool justYielded, bool isCatchup)
		{
			return !_psystem.IsAlive() || !_psystem.gameObject.activeSelf;
		}
	}
}