using System;
using Combat.Data.VFXs;
using Combat.Toolkit;
using UnityEngine;
using Util.UniTween.Value;

namespace Overworld.Cutscenes
{
	public class ManagedFade : CoroutineManaged
	{
		public float   targetOpacity = 0;
		public EaserTo easer         = new EaserTo();

		private readonly GameObject _gameObject;

		private TweenableFloat _opacity;
		private VFXManager     _vfxman;
		private ManualVFX      _vfx;


		public ManagedFade(GameObject gameObject)
		{
			_gameObject = gameObject;
			_vfx        = new ManualVFX();
			_opacity    = 1;
		}

		public override bool Active
		{
			get
			{
				bool active = _vfx != null && _vfx.IsActive;
				if (active && Math.Abs(targetOpacity - 1) < Mathf.Epsilon && Math.Abs(_vfx.Opacity - 1) < Mathf.Epsilon)
					// When this ManagedFade is a fade in, let's not hold onto it after it's done (there's no state to retain with the managed)
					return false;

				return active;
			}
		}

		public override void OnStart()
		{
			if (!_vfxman)
			{
				_vfxman = _gameObject.GetComponentInChildren<VFXManager>();
				if (!_vfxman)
				{
					this.LogError($"Cannot use with {_gameObject} because it's missing a VFXManager.");
					return;
				}
			}

			_vfxman.Add(_vfx);

			_opacity.CompleteIfTweening();
			_opacity.To(targetOpacity, easer);
		}

		public override void OnCoplayerUpdate(float dt)
		{
			base.OnCoplayerUpdate(dt);

			_vfx.opacity = _opacity.value;
		}

		public override void OnEnd(bool forceStopped, bool skipped = false)
		{
			_vfxman.Remove(_vfx);
		}

		public override bool CanContinue(bool justYielded, bool isCatchup)
		{
			return !_opacity.IsTweenActive;
		}
	}
}