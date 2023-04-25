using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Anjin.Util;
using Assets.Scripts.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Nanokin.Map
{
	public class TimedObjectsActivator : MonoBehaviour
	{
		public event Action<float> OnActivate;
		public event Action<int> OnTick;

		[Title("Functionality")]
		[SerializeField] private Transform _objectHolder;
		[SerializeField][ShowIf("@_objectHolder == null")] private List<TimedObject> _objects;
		[SerializeField] private float _duration;
		[SerializeField] [Range(0, 1)] private float _warnAtPercentLeft;
		[Title("Audio")]
		[SerializeField] private List<AudioDef> _tickSounds;
		[SerializeField] private AudioDef _expirationSound;
		[SerializeField] private float _tickIntervalSeconds;
		[SerializeField] private float _warningFrequencyIncrease;

		[Title("SFX")]
		[SerializeField] private List<ParticlePrefab> _collectionFX;

		private float _realTickInterval;

		private int _currentSFXindex = 0;
		private float _nextTickTime;
		private float _activationTime;
		private bool _isActive;

		public List<TimedObject> Objects => _objectHolder == null ?
			_objects : _objectHolder.GetComponentsInChildren<TimedObject>().ToList();

		private void Awake()
		{
			_isActive = false;
		}

		private void OnEnable()
		{
			_currentSFXindex = 0;
		}

		public void ActivateObjects()
		{
			_activationTime = Time.time;
			_nextTickTime = _activationTime;
			_isActive = true;
			foreach (var v in _collectionFX)
			{
				GameObject obj = v.Instantiate(transform);
			}
			OnActivate?.Invoke(Time.time);
			if (_objectHolder == null)
			{
				_objectHolder.SetActive(true);
				_objects.ForEach(x => x.Activate(this, _duration, _duration * _warnAtPercentLeft));
				return;
			}

			foreach (TimedObject obj in _objectHolder.GetComponentsInChildren<TimedObject>())
			{
				obj.Activate(this, _duration, _duration * _warnAtPercentLeft);
			}
		}

		private void Update()
		{
			if (!_isActive) return;
			_realTickInterval = (Time.time - _activationTime) < _duration * (1 - _warnAtPercentLeft) ?
				_tickIntervalSeconds : _tickIntervalSeconds * (1 / _warningFrequencyIncrease);
			if (Time.time >= _nextTickTime)
			{
				GameSFX.PlayGlobal(_tickSounds[_currentSFXindex++ % _tickSounds.Count]);
				while(_nextTickTime <= Time.time + Time.deltaTime)
					_nextTickTime += _realTickInterval;
				OnTick?.Invoke(_currentSFXindex);
			}

			if (Time.time - _activationTime >= _duration)
			{
				GameSFX.PlayGlobal(_expirationSound);
				_isActive = false;
			}
		}

		public void DisableSystem()
		{
			_isActive = false;
		}
	}
}