using Combat.Data.VFXs;
using DG.Tweening;
using UnityEngine;
using Util.UniTween.Value;

namespace Combat.Toolkit
{
	public class ColorInOutVFX : VFX
	{
		public readonly Color startColor;
		public readonly Color activeColor;
		public readonly Color endColor;
		public readonly bool  _isOverlay;

		private TweenableColor _color;
		private bool           _active;
		private EaserTo        _inTween, _outTween;

		public ColorInOutVFX(
			Color   color,
			EaserTo inTween,
			EaserTo outTween,
			bool    overlay    = false,
			Color?  startColor = null,
			Color?  endColor   = null)
		{
			this.activeColor = color;

			_inTween   = inTween;
			_outTween  = outTween;
			_isOverlay = overlay;

			this.startColor = startColor ?? (_isOverlay ? Color.clear : Color.white);
			this.endColor   = endColor ?? (_isOverlay ? Color.clear : Color.white);

			_color = new TweenableColor(this.startColor);
		}

		public override bool  IsActive => _active;
		public override Color Tint     => _isOverlay ? Color.white : _color.value;
		public override Color Fill  => !_isOverlay ? Color.clear : _color.value;

		internal override void Enter()
		{
			_active = true;

			_color.FromTo(startColor, activeColor, _inTween).SetVFX(this);
		}

		internal override void Leave()
		{
			_color.To(endColor, _outTween).SetVFX(this).OnComplete(() => _active = false);
		}
	}
}