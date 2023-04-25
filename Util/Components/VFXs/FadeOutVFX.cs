using DG.Tweening;
using Util.UniTween.Value;

namespace Combat.Toolkit
{
	public class FadeOutVFX : OneShotVFX
	{
		private readonly EaserTo        _ease;
		private          TweenableFloat _opacity = 0;

		public FadeOutVFX(float duration) : this(new EaserTo(duration, Ease.Linear)) { }

		public FadeOutVFX(EaserTo ease)
		{
			_ease = ease;
		}

		public override float Opacity => _opacity;

		internal override void Enter()
		{
			_opacity.To(0, _ease);
		}
	}
}