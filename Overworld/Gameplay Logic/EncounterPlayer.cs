using Anjin.Utils;
using Combat.Data.VFXs;
using Combat.Toolkit;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Nanokin.Park
{
	public class EncounterPlayer : SerializedMonoBehaviour
	{
		public bool Immune => _immuneDurationLeft > 0;

		private float _immuneDurationLeft = 0;

		private VFXManager   _vfxman;
		private BlinkVFX     _vfxBlink;
		private TimeScalable _timescale;

		private void Awake()
		{
			_vfxman    = GetComponentInChildren<VFXManager>();
			_timescale = GetComponent<TimeScalable>();
			_vfxBlink = new BlinkVFX
			{
				power = 5.5f,
				fill = ColorsXNA.LightCyan
			};
		}

		private void Update()
		{
			if (_immuneDurationLeft > 0)
			{
				_immuneDurationLeft -= _timescale.deltaTime;

				_vfxBlink.speed = 1;
				if (_immuneDurationLeft < 1.5f)
					_vfxBlink.speed = 2;

				if (_immuneDurationLeft < 0)
				{
					_vfxBlink.Leave();
				}
			}
		}

		[Button]
		public void AddImmunity(float duration)
		{
			if (duration <= float.Epsilon) return;

			if (_immuneDurationLeft <= Mathf.Epsilon && _vfxman != null)
				_vfxman.Add(_vfxBlink);

			_immuneDurationLeft += duration;
		}

		public void AddImmunityWithoutFlash(float duration)
		{
			//Debug.Log("IMMUNITY GRANTED; NO FLASH");

			if (duration <= float.Epsilon) return;

			/*if (_immuneDurationLeft <= Mathf.Epsilon && _vfxman != null)
				_vfxman.Add(_vfxBlink);*/

			_immuneDurationLeft += duration;
		}
	}
}