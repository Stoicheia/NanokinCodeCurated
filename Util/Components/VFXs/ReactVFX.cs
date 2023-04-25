using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Util;
using Anjin.Utils;
using Combat.Data.VFXs;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;
using Util.UniTween.Value;

namespace Combat.Toolkit
{
	/// <summary>
	/// A multi-purpose VFX with specific composition of effects.
	/// For example, ensures that shaking happens even during freeze frames.
	/// </summary>
	public class ReactVFX : VFX
	{
		// Flash
		// ----------------------------------------
		public bool  flash       = true;
		public int   flashFrames = 5;
		public Color flashColor  = Color.red.Lerp(Color.blue, 0.15f);

		// Tint
		// ----------------------------------------
		public bool    tint      = true;
		public EaserTo tintOut   = new EaserTo(0.5f, Ease.Linear);
		public Color   tintColor = Color.red.Lerp(Color.blue, 0.15f);

		// Shake
		// ----------------------------------------
		public bool  shake           = true;
		public int   shakeFrames     = 5;
		public float shakeAmplitude  = 0.16f;
		public float shakeSpeed      = 25;
		public float shakeRandomness = 0;

		// Recoil
		// ----------------------------------------
		public bool    recoil      = true;
		public EaserTo recoilIn    = new EaserTo(0.1f, Ease.OutFlash);
		public EaserTo recoilOut   = new EaserTo(0.11f, Ease.InOutQuad);
		public float   recoilForce = 1.3f;
		public float   recoilHold  = 0.15f;

		// Hurt frame
		// ----------------------------------------
		public bool  hurtAnim = true;
		public float hurtFrames = -1;

		// Freeze Frames
		// ----------------------------------------
		public int freezeFrames = 0;

		private float            _elapsedTotal; // Without any time scaling shanenigans
		private float            _elapsedSimulated;
		private TweenableVector3 _recoil;
		private TweenableColor   _tint;
		private float            _timeScale = 1;

		private int _frames = 0;

		private float _simulatedDuration;
		private float _totalDuration;

		public override bool IsActive => recoil && _recoil.IsTweenActive
		                                 || tint && _tint.IsTweenActive
		                                 || _elapsedSimulated < _simulatedDuration
		                                 || _elapsedTotal < _totalDuration;

		public override Vector3 VisualOffset
		{
			get
			{
				float shaking = 0;

				if (shake && _elapsedTotal <= shakeFrames * 1 / 60f)
				{
					float amplitude = shakeAmplitude + RNG.Float * shakeRandomness;
					shaking = Mathf.Cos(_elapsedTotal * shakeSpeed * Mathf.PI) * amplitude;
				}

				Vector3 shakeOffset  = GameCams.Live.UnityCam.transform.right * shaking;
				Vector3 recoilOffset = recoil ? _recoil.value : Vector3.zero;

				return shakeOffset + recoilOffset;
			}
		}

		[CanBeNull]
		public override string AnimSet => hurtAnim
		                                  && hurtFrames > 0
		                                  && _elapsedSimulated <= hurtFrames
			? "hurt"
			: null;

		public override Color Fill => _elapsedTotal < flashFrames * 1 / 60f ? flashColor : Color.clear;

		public override Color Tint => _tint;

		internal override void Enter()
		{
			base.Enter();

			ActorBase actor = gameObject.GetComponent<ActorBase>();

			if (freezeFrames > 0)
			{
				var go = new GameObject("Freeze Frame Volume");
				go.transform.position = go.transform.position;

				FreezeFrameVolume ff = go.AddComponent<FreezeFrameVolume>();
				ff.DurationFrames = freezeFrames;
				ff.SetGlobal();
			}

			if (tint)
			{
				_tint = new TweenableColor(tintColor);
				Tween tween = _tint.To(Color.white, tintOut);
				tween.timeScale = _timeScale;
				tween.SetVFX(this);
			}


			recoil = recoil && recoilForce > Mathf.Epsilon;
			if (recoil)
			{
				_recoil = new TweenableVector3();

				Sequence seq = DOTween.Sequence();
				seq.Append(_recoil.To(-actor.facing * recoilForce, recoilIn));
				seq.AppendInterval(recoilHold);
				seq.Append(_recoil.To(Vector3.zero, recoilOut));
				seq.SetVFX(this);
				seq.timeScale = _timeScale;

				_recoil.Sequence(seq);
			}

			_simulatedDuration = Mathf.Max(tintOut.duration, shakeFrames * 1 / 60f, recoilIn.duration + recoilOut.duration);
			_totalDuration     = Mathf.Max(flashFrames, freezeFrames) * 1 / 60f;
		}

		public override void OnTimeScaleChanged(float scale)
		{
			base.OnTimeScaleChanged(scale);

			_timeScale = scale;

			if (tint && _tint.activeTween != null) _tint.activeTween.timeScale       = scale;
			if (recoil && _recoil.activeTween != null) _recoil.activeTween.timeScale = scale;
		}

		public override void Update(float dt)
		{
			base.Update(dt);

			_elapsedTotal     += Time.deltaTime;
			_elapsedSimulated += dt * _timeScale;
		}

		[NotNull]
		public ReactVFX AllFrames(int nFrames)
		{
			freezeFrames = nFrames;
			shakeFrames  = nFrames;
			flashFrames  = nFrames;

			return this;
		}

		public override void OnAddingOverExisting(VFX existing)
		{
			base.OnAddingOverExisting(existing);

			var other = (ReactVFX)existing;
			if (recoil && other.recoil)
			{
				// Replace the existing recoil
				other._recoil.CompleteIfTweening();
				other._recoil.value = Vector3.zero;
			}
		}
	}
}