using DG.Tweening;
using JetBrains.Annotations;
using Overworld.Cutscenes;
using UnityEngine;
using Util.UniTween.Value;

namespace Combat.Toolkit.Camera
{
	public class CoroutineCamOrbit : CoroutineManaged
	{
		public float?  azimuth;
		public float?  elevation;
		public float?  distance;
		public EaserTo easer;
		public EaserTo initialEaser;

		private BattleRunner _runner;

		private EaserTo        _easer;
		private TweenableFloat _azimuth;
		private TweenableFloat _elevation;
		private TweenableFloat _distance;

		private int   _executions;
		private float _elapsed;
		private bool  _isActive;

		public CoroutineCamOrbit(float? azimuth   = null,
			float?                      elevation = null,
			float?                      distance  = null,
			Ease                        ease      = Ease.Linear,
			float                       duration  = 0.75f
		)
		{
			this.azimuth   = azimuth;
			this.elevation = elevation;
			this.distance  = distance;

			Tween(ease, duration);
		}

		public override void OnStart()
		{
			base.OnStart();


			_runner    = coplayer.state.battle;
			_elapsed = 0;

			_easer = _executions > 0 ? easer : initialEaser ?? easer;

			if (azimuth.HasValue)
			{
				_azimuth = _runner.camera.Orbit.Coordinates.azimuth;
				_azimuth.To(azimuth.Value, _easer);
			}

			if (elevation.HasValue)
			{
				_elevation = _runner.camera.Orbit.Coordinates.elevation;
				_elevation.To(elevation.Value, _easer);
			}

			if (distance.HasValue)
			{
				_distance = _runner.camera.Orbit.Coordinates.distance;
				_distance.To(distance.Value, _easer);
			}

			_isActive = distance.HasValue || elevation.HasValue || azimuth.HasValue;
			_executions++;
		}

		public override void OnEnd(bool forceStopped , bool skipped = false)
		{
			_isActive = false;
			// if (azimuth.HasValue) _core.camera.Orbit.Coordinates.azimuth     = azimuth.Value;
			// if (elevation.HasValue) _core.camera.Orbit.Coordinates.elevation = elevation.Value;
			// if (distance.HasValue) _core.camera.Orbit.Coordinates.distance   = distance.Value;
		}

		[NotNull]
		public CoroutineCamOrbit Tween(Ease ease, float duration)
		{
			easer = new EaserTo(duration, ease);
			return this;
		}

		[NotNull]
		public CoroutineCamOrbit InitialTween(Ease ease, float duration)
		{
			initialEaser = new EaserTo(duration, ease);
			return this;
		}

		public override float ReportedDuration => _easer.duration;

		public override float ReportedProgress => _elapsed / _easer.duration;

		public override bool Active => _isActive;

		public override void OnCoplayerUpdate(float dt)
		{
			base.OnCoplayerUpdate(dt);

			_elapsed += Time.deltaTime;

			if (_elapsed > _easer.duration)
				_isActive = false;

			float timescale = costate.timescale.current;

			if (_azimuth != null)
			{
				_runner.camera.Orbit.Coordinates.azimuth = _azimuth.value;
				if (_azimuth.activeTween != null)
					_azimuth.activeTween.timeScale = timescale;
			}

			if (_elevation != null)
			{
				_runner.camera.Orbit.Coordinates.elevation = _elevation.value;
				if (_elevation.activeTween != null)
					_elevation.activeTween.timeScale = timescale;
			}

			if (_distance != null)
			{
				_runner.camera.Orbit.Coordinates.distance = _distance.value;
				if (_distance.activeTween != null)
					_distance.activeTween.timeScale = timescale;
			}
		}
	}
}