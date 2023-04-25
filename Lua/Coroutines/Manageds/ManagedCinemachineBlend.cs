using Anjin.Cameras;
using Cinemachine;
using Overworld.Cutscenes;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class ManagedCinemachineBlend : CoroutineManaged
	{
		public static readonly ManagedCinemachineBlend Instance = new ManagedCinemachineBlend();

		public override bool Active => GameCams.Live.Brain.IsBlending || !_started;

		private bool _started;
		private int  _safteyTimer;

		public override void OnStart()
		{
			base.OnStart();
			GameCams.Live.Update();
			GameCams.Live.Brain.ManualUpdate();
			_started     = false;
			_safteyTimer = 0;
		}

		public override void OnCoplayerUpdate(float dt)
		{
			//base.OnCoplayerUpdate(dt);
			if(GameCams.Live.Brain.IsBlending || _safteyTimer >= 5) {
				_started = true;
			} else {
				_safteyTimer++;
			}
		}

		public override void OnEnd(bool forceStopped, bool skipped = false)
		{
			if (skipped) {
				GameCams.Live.Update();

				GameCams.Live.Brain.ManualUpdate();
				GameCams.Live.Brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0);
				GameCams.Live.Brain.ManualUpdate();
			}
		}
	}
}