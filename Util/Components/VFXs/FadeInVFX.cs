using DG.Tweening;
using Util.UniTween.Value;

namespace Combat.Toolkit
{
	public class FadeInVFX : OneShotVFX
	{
		private EaserTo        _ease;
		private TweenableFloat _opacity = 1;

		private Tween _tween;

		public FadeInVFX(float duration) : this(new EaserTo(duration, Ease.Linear)) { }

		public FadeInVFX(EaserTo ease)
		{
			_ease = ease;
		}

		public override bool  IsActive => _tween.active;
		public override float Opacity  => _opacity;

		internal override void Enter()
		{
			_tween = _opacity.To(1, _ease);
		}
	}
}