using Combat.Data.VFXs;
using DG.Tweening;
using UnityEngine;
using UnityUtilities;
using Util.UniTween.Value;

namespace Combat.Toolkit
{
	public class DimInOutVFX : VFX
	{
		private readonly float   _dimming;
		private readonly EaserTo _in, _out;

		private TweenableFloat _current = 0;
		private bool           _isActive;

		public DimInOutVFX(float dimming, EaserTo @in, EaserTo @out)
		{
			_dimming = dimming;
			_in      = @in;
			_out     = @out;
		}

		public override bool  IsActive => _isActive;
		public override Color Fill  => Color.black.Alpha(_current);

		internal override void Enter()
		{
			_isActive = true;
			_current.To(_dimming, _in).SetVFX(this);
		}

		internal override void Leave()
		{
			_current.To(0, _out).OnComplete(() => _isActive = false).SetVFX(this);
		}
	}
}