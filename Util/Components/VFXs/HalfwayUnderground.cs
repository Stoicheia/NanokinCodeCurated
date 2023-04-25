using Anjin.Actors;
using Combat.Data.VFXs;
using DG.Tweening;
using UnityEngine;
using Util.UniTween.Value;

namespace Combat.Toolkit
{
	public class HalfwayUnderground : VFX
	{
		private Config           _config;
		private TweenableVector3 _offset = new TweenableVector3();
		private bool             _isActive;

		public HalfwayUnderground() : this(new Config()) { }

		public HalfwayUnderground(Config config)
		{
			_config = config;
		}

		public override bool    IsActive     => _isActive;
		public override Vector3 VisualOffset => _offset;

		internal override void Enter()
		{
			_isActive = true;
			ActorBase actor = gameObject.GetComponent<ActorBase>();

			_offset
				.To(_config.distance * actor.height * Vector3.down, _config.diveIn)
				.SetVFX(this);
		}

		internal override void Leave()
		{
			_offset
				.To(Vector3.zero, _config.popOut)
				.OnComplete(() => _isActive = false)
				.SetVFX(this);
		}

		public class Config
		{
			public float   distance = 0.4f;
			public EaserTo diveIn   = new EaserTo(0.09f, Ease.OutQuad);
			public EaserTo popOut   = new EaserTo(0.25f, Ease.OutQuad);
		}
	}
}