using System;
using Cinemachine;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;
using Util.UniTween.Value;

namespace Util.Components.Cinemachine
{
	public class CinemachineNoiseController : MonoBehaviour
	{
		public float BaseAmplitude;

		[ShowInPlay, NonSerialized] public TweenableFloat Amplitude;
		[ShowInPlay, NonSerialized] public TweenableFloat Frequency;

		public const float DEFAULT_AMPLITUDE = 1;

		[ShowInPlay] private bool  _shaking;
		[ShowInPlay] private float _timer;
		[ShowInPlay] private float _duration;
		[ShowInPlay] private float _amplitude;

		[ShowInPlay] private CinemachineVirtualCamera           _cam;
		[ShowInPlay] private CinemachineBasicMultiChannelPerlin _noise;


		private void Awake()
		{
			_cam   = GetComponent<CinemachineVirtualCamera>();
			_noise = _cam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

			if (_noise == null)
			{
				_noise                 = _cam.AddCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
				_noise.m_AmplitudeGain = 0;
				_noise.m_NoiseProfile  = GameAssets.Live.CamNoiseSettings;
			}

			Amplitude = _noise.m_AmplitudeGain;
			Frequency = _noise.m_FrequencyGain;
		}

		private void Update()
		{
			if (_shaking)
			{
				Amplitude =  (1 + Mathf.Cos(_timer / _duration * Mathf.PI)) / 2 * _amplitude;
				_timer    += Time.deltaTime;
				if (_timer >= _duration)
				{
					_shaking = false;
					_timer   = 0;
				}
			}
			else
			{
				Amplitude = BaseAmplitude;
			}

			_noise.m_AmplitudeGain = Amplitude;
			_noise.m_FrequencyGain = Frequency;
		}

		[Button]
		public void DoShake(float duration = 0.75f, float amplitude = DEFAULT_AMPLITUDE)
		{
			_shaking   = true;
			_duration  = duration;
			_timer     = 0;
			_amplitude = amplitude;
		}

		public class Managed : CoroutineManaged
		{
			public          CinemachineNoiseController controller;
			public override bool                       Active => controller._shaking;

			public override bool Skippable => true;

			public override void OnEnd(bool forceStopped, bool skipped = false)
			{
				controller._shaking  = false;
				controller.Amplitude = 0;
			}
		}
	}
}