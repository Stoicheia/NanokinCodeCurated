using System;
using Anjin.Actors;
using Combat.Data.VFXs;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;
using Util.UniTween.Value;

namespace Combat.Toolkit
{
	public class RecoilVFX : OneShotVFX
	{
		private readonly Config _config;

		private Vector3          _direction;
		private TweenableVector3 _offset = new TweenableVector3();

		public RecoilVFX(Config config)
		{
			_config = config;
		}

		public override Vector3 VisualOffset => _offset;

		public RecoilVFX(float force) : this(new Config())
		{
			_config.force = force;
		}

		[NotNull]
		public override string AnimSet => "hurt";

		public override void OnTimeScaleChanged(float scale)
		{
			base.OnTimeScaleChanged(scale);

			if (_offset.activeTween != null)
				_offset.activeTween.timeScale = scale;
		}

		internal override void Enter()
		{
			ActorBase actor = gameObject.GetComponent<ActorBase>();

			_direction = _config.direction ?? -actor.facing;

			Sequence seq = DOTween.Sequence();
			seq.Append(_offset.To(_direction * _config.force, _config.recoilEase).SetVFX(this));
			seq.Append(_offset.To(Vector3.zero, _config.pullbackEase).SetVFX(this));

			_offset
				.Sequence(seq)
				.OnComplete(OnEnded)
				.SetVFX(this);
		}

		[Serializable]
		public class Config
		{
			public float    force = 0.65f;
			public Vector3? direction;

			public EaserTo recoilEase   = new EaserTo(0.1f, Ease.OutFlash).Delay(0.03f);
			public EaserTo pullbackEase = new EaserTo(0.11f, Ease.InOutQuad).Delay(0.15f);
		}
	}
}