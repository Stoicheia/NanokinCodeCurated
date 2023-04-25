using Anjin.Scripting;
using Anjin.Scripting.Waitables;
using MoonSharp.Interpreter;

namespace Overworld.Cutscenes
{
	public class CutsceneLuaProxy : MonoLuaProxy<Cutscene> {
		public bool             playing                        => proxy.playing;
		public WaitableCutscene play()                         	=> proxy.Play();
		public WaitableCutscene play(Closure     func)         	=> proxy.Play(func);
		public void             end_with(Closure func)         	=> proxy.EndWith(func);
		public WaitableCutscene play_as_child(Cutscene parent)  => proxy.PlayAsChild(parent);
        public bool             controls_cam => proxy.ControlsCamera;
    }
}