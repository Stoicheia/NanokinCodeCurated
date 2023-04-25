using UnityEngine;
using Util.UniTween.Value;

namespace Combat.Toolkit
{
	public class FadeColorVFX : OneShotVFX
	{
		private readonly EaserTo _exit;

		private TweenableColor _currentColor;

		public FadeColorVFX(Color color, EaserTo exit)
		{
			_currentColor = color;
			_exit         = exit;
		}

		public override Color Tint => _currentColor;

		internal override void Enter()
		{
			tweens.Add(_currentColor.To(Color.white, _exit));
		}
	}
}