using System;
using Anjin.Utils;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;
using Util.Odin.Attributes;

namespace Anjin.Nanokin.Park
{
	[AddComponentMenu("Anjin: Game Building/Stunnable")]
	public class Stunnable : SerializedMonoBehaviour
	{
		[SerializeField, SuffixLabel("sec")] private ManualTimer HealDuration = -1;
		[Space]
		[SerializeField, Optional] private GameObject StunParticlePrefab;
		[SerializeField] private Vector3 StunParticlePosition;

		[ShowInPlay]
		public bool Stunned
		{
			get => _stunned;
			set
			{
				bool changed = _stunned != value;
				_stunned = value;

				if (_stunned) HealDuration.Restart();
				else HealDuration.Stop();

				if (changed)
				{
					if (_hasParticles)
						_particles.SetActive(value);

					if (_stunned)
						onStunned?.Invoke();
				}
			}
		}

		public Action onStunned;

		private bool  _stunned;
		private float _elapsedTime;

		private GameObject _particles;
		private bool       _hasParticles;

		private TimeScalable _timeScalable;
		private bool         _hasTimeScalable;

		private void Awake()
		{
			if (StunParticlePrefab != null)
			{
				_particles = Instantiate(StunParticlePrefab, transform);

				_particles.transform.localPosition = StunParticlePosition;
				_particles.SetActive(false);

				_hasParticles = true;
			}

			_hasTimeScalable = TryGetComponent(out _timeScalable);
		}

		private void OnEnable()
		{
			Stunned = false;
		}

		private void Update()
		{
			HealDuration.Update(Time.deltaTime * (_hasTimeScalable ? _timeScalable.current : 1));

			if (HealDuration.JustEnded)
			{
				Stunned = false;
			}
		}

		private void OnDrawGizmosSelected()
		{
			Draw2.DrawWireSphere(transform.position + StunParticlePosition, 0.1f, Color.yellow);
		}
	}
}