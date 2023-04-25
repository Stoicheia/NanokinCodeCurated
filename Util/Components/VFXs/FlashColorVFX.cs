using Anjin.Actors;
using Anjin.Util;
using Combat.Data.VFXs;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;
using UnityUtilities;
using Util.UniTween.Value;

namespace Combat.Toolkit
{
	public class FlashColorVFX : VFX
	{
		//public override Color Fill { get; }

		// Options
		// ----------------------------------------
		public float power;
		public float speed;
		public Color fill = Color.white;
		public Color tint = Color.clear;

		// Flash
		// ----------------------------------------
		public bool flash = true;
		//public Color flashColor = Color.red.Lerp(Color.blue, 0.15f);

		// Tint
		// ----------------------------------------
		public EaserTo tintOut = new EaserTo(0.5f, Ease.Linear);
		//public Color tintColor = Color.red.Lerp(Color.blue, 0.15f);

		private float _elapsedTotal; // Without any time scaling shanenigans
		//private float _elapsedSimulated;
		//private TweenableColor _tint;
		private float _timeScale = 1;

		public int frames = 0;

		private float _simulatedDuration;
		private float _totalDuration;

		private Color _fill;
		private Color _tint;
		private float _emission;

		public override bool IsActive => /*(((_tint != null) && _tint.IsTweening)
								 || (_elapsedSimulated < _simulatedDuration) 
								 ||*/ (_elapsedTotal < _totalDuration); //);

		//public override Color Fill => ((_elapsedTotal < (flashFrames * 1 / 60f)) ? flashColor : Color.clear);
		public override Color Fill => _fill;

		//public override Color Tint => _tint;

		public override Color Tint => _tint;

		public FlashColorVFX(int frames = 0, float power = 1f, float speed = 1f, Color fill = default(Color), Color tint = default(Color))
		{
			this.frames = frames;
			this.power = power;
			this.speed = speed;
			this.fill = ((fill != default(Color)) ? fill : Color.white);
			this.tint = ((tint == default(Color)) ? Color.clear : tint);
		}

		internal override void Enter()
		{
			base.Enter();

			ActorBase actor = gameObject.GetComponent<ActorBase>();

			//_tint = new TweenableColor(tintColor);
			//Tween tween = _tint.To(Color.white, tintOut);
			//tween.timeScale = _timeScale;
			//tween.SetVFX(this);

			_simulatedDuration = tintOut.duration * 1 / 60f;
			_totalDuration = frames * speed / 60f;
		}

		public override void OnTimeScaleChanged(float scale)
		{
			base.OnTimeScaleChanged(scale);

			_timeScale = scale;

			//if ((_tint != null) && (_tint.activeTween != null)) _tint.activeTween.timeScale = scale;
		}

		public override void Update(float dt)
		{
			//_elapsedSimulated += dt * _timeScale;

			if (IsActive)
			{
				base.Update(dt);

				_elapsedTotal += Time.deltaTime * speed;

				float amp = Mathf.Sin(_elapsedTotal * 16 * speed) * 0.5f + 0.5f;

				_fill = fill.Alpha(amp * 0.105f * power);
				_tint = Color.Lerp(Color.white, tint, amp * 0.105f * power);
				_emission = amp * 0.525f * power;
			}
		}

		[NotNull]
		public FlashColorVFX AllFrames(int nFrames)
		{
			frames = nFrames;

			return this;
		}
	}
}