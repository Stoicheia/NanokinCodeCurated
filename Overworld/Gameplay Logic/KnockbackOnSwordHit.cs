using System;
using Anjin.Actors;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;

namespace Anjin.Nanokin.Park
{
	[AddComponentMenu("Anjin: Events/Knockback on Sword Hit")]
	public class KnockbackOnSwordHit : SerializedMonoBehaviour, IHitHandler<SwordHit>
	{
		public float KnockbackForce  = 3;
		public float KnockbackHeight = 1;

		[NonSerialized]
		public Action<SwordHit> OnHitCallback;

		public void OnHit(SwordHit info)
		{
			if (gameObject.TryGetComponent(out ActorKCC motor))
			{
				float yForce = MathUtil.CalculateJumpForce(KnockbackHeight * motor.GetJumpHeightModifier(), motor.Gravity);
				motor.AddForce(Vector3.up * yForce + info.direction * (KnockbackForce * info.force), setXZ: true);
			} else if (gameObject.TryGetComponent(out Rigidbody rb))
			{
				float yForce = MathUtil.CalculateJumpForce(KnockbackHeight * motor.GetJumpHeightModifier(), Physics.gravity.y);
				rb.AddForce(Vector3.up * yForce + info.direction * (KnockbackForce * info.force));
			}

			OnHitCallback?.Invoke(info);
		}

		public bool IsHittable(SwordHit hit) => true;
	}
}