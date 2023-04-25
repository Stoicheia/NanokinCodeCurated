using Overworld.Cutscenes;

namespace Combat.Toolkit
{
	public class WaitAnimation : CoroutineManaged
	{
		private readonly float _duration;

		private float _speed = 1, _elapsed;

		public WaitAnimation(float duration)
		{
			_duration = duration;
		}

		public override bool Active => _elapsed < _duration;

		public override float ReportedProgress => _elapsed / _duration;

		public override float ReportedDuration => _duration;

		public override void OnCoplayerUpdate(float dt)
		{
			_elapsed += dt;
		}
	}
}