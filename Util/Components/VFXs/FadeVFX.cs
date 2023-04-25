using Combat.Data.VFXs;
using DG.Tweening;
using Util.UniTween.Value;

namespace Combat.Toolkit
{
	/// <summary>
	/// Fading means going transparent, 1 is fully transparent.
	/// </summary>
	public class FadeVFX : VFX
	{
		private readonly float   _fading;
		private readonly EaserTo _in, _out;

		private TweenableFloat _current = 0;
		private bool           _isActive;

		/// <param name="fading">Value from 0 to 1, where 0 corresponds to 100% opacity and 1 to 0% (completely invisible)</param>
		/// <param name="in">in tween for the fade.</param>
		/// <param name="out">out tween for the fade.</param>
		public FadeVFX(float fading, EaserTo @in, EaserTo @out)
		{
			_fading = fading;
			_in     = @in;
			_out    = @out;
		}

		public override bool  IsActive => _isActive;
		public override float Opacity  => _current;

		internal override void Enter()
		{
			_isActive = true;
			_current.To(1 - _fading, _in).SetVFX(this);
		}

		internal override void Leave()
		{
			_current.To(1, _out).OnComplete(() => _isActive = false).SetVFX(this);
		}
	}
}