using Anjin.Util;
using Combat.Data.VFXs;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;
using Util.UniTween.Value;

namespace Combat.Toolkit
{
	public class AirbornVFX : VFX
	{
		private readonly Config _config;

		[ShowInInspector, Inline(true)]
		private TweenableVector3 _visualOffset = new TweenableVector3();

		private bool _isActive;

		public AirbornVFX(Config config)
		{
			_config = config;
		}

		public override bool    IsActive     => _isActive;
		public override Vector3 VisualOffset => _visualOffset.value;

		internal override void Enter()
		{
			_isActive = true;
			_visualOffset
				.To(Vector3.up * _config.height, _config.enter)
				.SetVFX(this);
		}

		internal override void Leave()
		{
			_visualOffset.Sequence(
					_visualOffset.To(Vector3.zero, _config.leave).OnComplete(OnLand),
					_visualOffset.To(Vector3.zero, _config.landBounce).OnComplete(() => _isActive = false)
				)
				.SetVFX(this);
		}

		private void OnLand()
		{
			_config.landParticlePrefab.InstantiateOptional(gameObject.transform.position + Vector3.up * _config.landParticlesHeight);
			GameSFX.Play(_config.landSound, gameObject.transform);
			// TODO
			// EntityLanded?.Invoke(view);
		}

		// TODO
		// public AirbornVFX OnLanded(EntityEventHandler action)
		// {
		// EntityLanded += action;
		// return this;
		// }

		public class Config
		{
			public float      height = 15;
			public EaserTo    enter  = new EaserTo(0.5f, Ease.Linear);
			public EaserTo    leave  = new EaserTo(0.5f, Ease.Linear);
			public GameObject landParticlePrefab;
			public float      landParticlesHeight = 1.2f;
			public AudioDef   landSound           = new AudioDef();
			public JumperTo   landBounce          = new JumperTo(0.33f, 1.1f);
		}
	}
}