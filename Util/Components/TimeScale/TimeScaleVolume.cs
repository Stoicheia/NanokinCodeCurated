using System;
using DG.Tweening;
using UnityEngine;
using Util.UniTween.Value;

namespace Anjin.Utils
{
	/// <summary>
	/// A simple time scale volume where the scaling is set externally.
	/// </summary>
	public class TimeScaleVolume : TimeScaleVolumeBase
	{
		public float Value = 1f;

		private TweenableFloat _tweenableValue;

		private bool           _enableCurve;
		private AnimationCurve _curve;
		private float          _progress;
		private bool           _destroy;

		public override float Scaling => _tweenableValue ?? Value;

		public static TimeScaleVolume Spawn(
			string  name,
			Vector3 position,
			float   radius    = 100,
			int     layerMask = int.MaxValue)
		{
			var obj = new GameObject(name);
			obj.transform.position = position;

			SphereCollider collider = obj.AddComponent<SphereCollider>();
			collider.radius    = radius;
			collider.isTrigger = true;

			TimeScaleVolume volume = obj.AddComponent<TimeScaleVolume>();
			volume.Value     = 1;
			volume.LayerMask = layerMask;

			return volume;
		}

		public Tween Tween(float from, float to, EaserTo ease = null)
		{
			_tweenableValue = _tweenableValue ?? new TweenableFloat();
			return _tweenableValue.FromTo(from, to, ease);
		}

		public Tween TweenAndDestroy(float from, float to, EaserTo ease = null)
		{
			_tweenableValue = _tweenableValue ?? new TweenableFloat();
			return _tweenableValue
				.FromTo(from, to, ease)
				.OnUpdate(() => Value = _tweenableValue)
				.OnComplete(() => Destroy(gameObject));
		}


		public void Tween(AnimationCurve curve)
		{
			_enableCurve = true;
			_curve       = curve;
			_progress    = 0;
		}

		public void TweenAndDestroy(AnimationCurve curve)
		{
			_enableCurve = true;
			_curve       = curve;
			_progress    = 0;
			_destroy     = true;
		}

		protected override void Update()
		{
			if (_enableCurve)
			{
				_progress += Time.deltaTime;
				Value     =  _curve.Evaluate(_progress);
				if (_progress > _curve[_curve.length - 1].time && _destroy)
				{
					Destroy(gameObject);
				}
			}

			base.Update();
		}
	}
}