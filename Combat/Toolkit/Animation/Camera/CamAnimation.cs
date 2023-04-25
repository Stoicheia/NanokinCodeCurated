using MoonSharp.Interpreter;
using Overworld.Cutscenes;

namespace Combat.Toolkit.Camera
{
	public class CamAnimation : CoroutineManaged
	{
		private readonly Closure     _closure;
		private          int         _maxRepeats;
		private          ArenaCamera _camera;
		private          bool        _active;

		public CamAnimation(Closure closure, int maxRepeats)
		{
			_closure    = closure;
			_maxRepeats = maxRepeats;
		}

		public override bool  Active           => _active;
		public override float ReportedDuration => 0.5f;
		public override float ReportedProgress => 0.5f;

		// public override float PlaySpeed
		// {
		// 	set { }
		// }

		public override void OnStart()
		{
			_camera = coplayer.state.battle?.io.arena.Camera;
			if (_camera == null) return;

			if (_closure != null)
			{
				_camera.Play(coplayer.script, _closure, _maxRepeats);
				_camera.coplayer.beforeCompleteTmp += OnBeforeCompleteTmp;
			}
		}

		private void OnBeforeCompleteTmp()
		{
			_active = false;
		}
	}
}