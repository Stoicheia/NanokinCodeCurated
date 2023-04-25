using Anjin.Util;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util;

namespace Combat.UI
{
	public class DamageNumber : MonoBehaviour
	{
		[Range(0, 9999)]
		public int damageValue;

		public bool EnableMovement;

		[FormerlySerializedAs("objectToMove")]
		[Required]
		[ShowIf("EnableMovement")]
		public GameObject ObjectToMove;

		[FormerlySerializedAs("movementSpeedMax")]
		[ShowIf("EnableMovement")]
		public FloatRange MovementHorizontalSpeed;

		[FormerlySerializedAs("movementSpeedMax")]
		[ShowIf("EnableMovement")]
		public FloatRange MovementVerticalSpeed;

		[ShowIf("EnableMovement")]
		public FloatRange MovementAngleRange = new FloatRange(20, 45);

		[FormerlySerializedAs("bounceFriction"), Range(0f, 1f)]
		[ShowIf("EnableMovement")]
		public float BounceFriction;

		// private Vector3 _movementDir; // 2D plane to move across
		private Vector3 _velocity; //
		private Vector3 _position; // Position on the plane

		public virtual void SetDamageNumber(int damage)
		{
			int shownDamage = Mathf.Clamp(damage, 0, 9999);
			foreach (DamageNumberChild damageNumber in GetComponentsInChildren<DamageNumberChild>())
			{
				damageNumber.SetDamageNumber(shownDamage);
			}
		}

		public virtual void OnAnimationEnter()
		{
			if (EnableMovement)
			{
				// _movementDir = transform.up + transform.right.Rotate(y: MovementAngleRange.RandomInclusive);
				_velocity = Vector3.zero;
				_position = Vector3.zero;
				_velocity = transform.up * MovementVerticalSpeed.RandomInclusive +
				            transform.forward.Rotate(y: MovementAngleRange.RandomInclusive) * MovementHorizontalSpeed.RandomInclusive;
			}
		}

		public virtual void OnAnimationExit()
		{
			Destroy(gameObject);
		}

		[UsedImplicitly]
		public void OnBounce()
		{
			_velocity *= BounceFriction;
		}

		public void OnRest()
		{
			_velocity = Vector3.zero;
		}

		private void Update()
		{
			if (EnableMovement)
			{
				_position += _velocity * Time.deltaTime;

				ObjectToMove.transform.localPosition = _position;
			}
		}

		// public virtual void OnValidate()
		// {
		// 	if (!Application.isPlaying)
		// 	{
		// 		SetDamageNumber(damageValue);
		// 	}
		// }
	}
}