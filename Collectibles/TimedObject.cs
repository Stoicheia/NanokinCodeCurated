using System;
using System.Collections;
using System.Collections.Generic;
using Anjin.Util;
using Anjin.Utils;
using Assets.Scripts.Utils;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Anjin.Nanokin.Map
{
	public class TimedObject : MonoBehaviour
	{
		private const float BLINK_IN_TIME = 0.65f;

		private TimedObjectsActivator _myActivator;
		private Collider _col;
		[SerializeField] private List<Transform> _graphicObjects;
		[SerializeField] private ParticlePrefab _spawnFX;
		private enum State {Deactivated, Activated, Critical}

		private float _duration;
		private float _criticalTime;
		private float _timer;
		private State _state;

		[SerializeField] private bool _blinkWhenCritical;
		private BlinkOverTime _blinkEffect;

		private void Awake()
		{
			_col = GetComponent<Collider>();
			_blinkEffect = GetComponent<BlinkOverTime>();
			Deactivate();
		}

		public void Activate(TimedObjectsActivator a, float dur, float crit)
		{
			_myActivator = a;
			_duration = dur;
			_criticalTime = crit;
			_timer = dur;
			_state = State.Activated;
			_graphicObjects.ForEach(x => x.SetActive(true));
			_col.enabled = true;
			_spawnFX.Instantiate(transform);
			if(_blinkWhenCritical && _blinkEffect != null)
				_blinkEffect.enabled = false;
		}

		private void Update()
		{
			if(_state != State.Deactivated) _timer -= Time.deltaTime;
			CheckBlink();
			if (_timer <= 0) Deactivate();
		}

		private void CheckBlink()
		{
			if(_blinkEffect !=null && _blinkWhenCritical)
				_blinkEffect.enabled = _timer >= _duration - BLINK_IN_TIME || _timer < _criticalTime;
			if (_timer <= _criticalTime && _state == State.Activated) _state = State.Critical;
		}

		private void Deactivate()
		{
			_col.enabled = false;
			_graphicObjects.ForEach(x => x.SetActive(false));
			_state = State.Deactivated;
			if(_blinkWhenCritical && _blinkEffect != null)
				_blinkEffect.enabled = false;
		}
	}
}
