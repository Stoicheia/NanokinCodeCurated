using Overworld.Cutscenes;
using UnityEngine;
using Util.Extensions;

namespace Combat.Toolkit
{
	public class UnityAnimation : CoroutineManaged
	{
		private Animation     _animator;
		private AnimationClip _clip;

		public UnityAnimation(Animation animator, AnimationClip clip)
		{
			_animator = animator;
			_clip     = clip;
		}

		public override float ReportedDuration => _clip.length;

		public override float ReportedProgress => _animator[_clip.name].normalizedTime;

		public override bool Active => _animator.isPlaying;

		public override void OnCoplayerUpdate(float dt)
		{
			base.OnCoplayerUpdate(dt);

			foreach (AnimationState state in _animator)
			{
				state.speed = costate.timescale.current;
			}
		}

		public override void OnStart()
		{
			base.OnStart();
			_animator.PlayClip(_clip);
		}

		public override void OnEnd(bool forceStopped, bool skipped = false)
		{
			base.OnEnd(forceStopped);
			_animator.Stop();
		}
	}
}