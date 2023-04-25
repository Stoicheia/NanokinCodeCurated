using DG.Tweening;
using UnityEngine;
using Util.Components.Cinemachine;

namespace Combat.Data.Decorative
{
	public class PunchCameraEffect : ProcEffect
	{
		private readonly float _force;
		private readonly float _duration;
		private readonly float _elasticity;
		private readonly int   _vibrato;
		private          Ease  _ease;

		public PunchCameraEffect(float force      = 1.7f,
			float                      duration   = 0.1f,
			float                      elasticity = 0,
			int                        vibrato    = 0,
			Ease                       ease       = Ease.InOutQuad
		)
		{
			_force      = force;
			_duration   = duration;
			_elasticity = elasticity;
			_vibrato    = vibrato;
			_ease       = ease;
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			var tweenOrbit = new AdditiveTweenOrbit();

			Tween tween = DOTween.Punch(
					() => tweenOrbit.coordinate.Vector,
					vec3 => tweenOrbit.coordinate.Vector = vec3,
					new Vector3(0, 0, 1) * _force,
					_duration,
					_vibrato,
					_elasticity
				)
				.SetEase(_ease);

			tweenOrbit.RemoveOnComplete(tween);

			battle.runner.camera.Orbit.AddAdditiveOrbit(tweenOrbit);


			return ProcEffectFlags.DecorativeEffect;
		}
	}
}