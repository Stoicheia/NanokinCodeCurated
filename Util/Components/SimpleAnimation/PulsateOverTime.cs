using System;
using System.Collections;
using System.Collections.Generic;
using Anjin.Nanokin.Map;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Utils
{
	public class PulsateOverTime : MonoBehaviour
	{
		private enum PulseType {Trigonometric, Linear, Discrete}

		[SerializeField] private float _scale;
		[SerializeField][Range(0.01f, 100)] [ShowIf("@!_respondToTimedObjectTicks")]
		private float _period;
		[SerializeField] private PulseType _pulseType;

		[SerializeField] [ShowIf("@_pulseType == PulseType.Discrete && !_respondToTimedObjectTicks")]
		private float _discretePulseNumber;

		[SerializeField] [ShowIf("@_pulseType == PulseType.Discrete")]
		private bool _respondToTimedObjectTicks;

		[SerializeField]
		private TimedObjectsActivator _reactToObject;

		[SerializeField] private bool _fixedPhase;
		private Vector3 _initialScale;
		private Vector3 _upperScale;
		private float _startTime;

		private void Awake()
		{
			_initialScale = transform.localScale;
		}

		private void OnEnable()
		{
			ResetPhase(Time.time);
			if (_reactToObject != null)
			{
				_reactToObject.OnTick += HearTick;
				_reactToObject.OnActivate += ResetPhase;
			}
		}

		private void OnDisable()
		{
			if (_reactToObject != null)
			{
				_reactToObject.OnTick -= HearTick;
				_reactToObject.OnActivate -= ResetPhase;
			}
		}

		private void Update()
		{
			if (_respondToTimedObjectTicks) return;
			CalculateScaleNormally();
		}

		private void CalculateScaleNormally()
		{
			_upperScale = _initialScale * _scale;
			float pulsePosition;
			float offsetFactor = Convert.ToInt32(_fixedPhase) * _startTime;
			float t = Time.time - offsetFactor;
			switch (_pulseType)
			{
				case PulseType.Trigonometric:
					pulsePosition = (Mathf.Sin(2*Mathf.PI*(1/_period)*t) + 1)/2;
					break;
				case PulseType.Linear:
					pulsePosition = 1-2*Mathf.Abs(t/_period % 1 - 0.5f);
					break;
				default:
					pulsePosition = (Mathf.Floor(t * _period) / _period % 1) * _period / (_period - 1);
					break;
			}

			transform.localScale = Vector3.Lerp(_initialScale, _upperScale, pulsePosition);
		}

		private void HearTick(int i)
		{
			if (!_respondToTimedObjectTicks) return;
			_upperScale = _initialScale * _scale;
			transform.localScale = i % 2 == 0 ? _initialScale : _upperScale;
		}

		private void ResetPhase(float t)
		{
			_startTime = t;
		}
	}
}
