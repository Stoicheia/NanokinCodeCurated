using Overworld.Cutscenes;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class WaitableCutscene : ICoroutineWaitable
	{
		public Cutscene Cutscene;

		public WaitableCutscene(Cutscene cutscene) => Cutscene = cutscene;
		public bool CanContinue(bool     justYielded, bool isCatchup) => !Cutscene.playing;
	}
}