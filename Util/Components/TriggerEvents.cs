using System;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Util.Components
{
	[AddComponentMenu("Anjin: Events/Contact Events")]
	public class CollisionEvents : SerializedMonoBehaviour
	{
		[Title("Events")]
		[NonSerialized, ShowInPlay, PropertyOrder(3)] public CollisionEvent onCollisionEnter;
		[NonSerialized, ShowInPlay, PropertyOrder(3)] public CollisionEvent onCollisionStay;
		[NonSerialized, ShowInPlay, PropertyOrder(3)] public CollisionEvent onCollisionExit;

		public delegate void CollisionEvent(Collision collision);

		private void OnCollisionEnter(Collision collision)
		{
			onCollisionEnter?.Invoke(collision);
		}

		private void OnCollisionStay(Collision collision)
		{
			onCollisionStay?.Invoke(collision);
		}

		private void OnCollisionExit(Collision collision)
		{
			onCollisionExit?.Invoke(collision);
		}
	}

	[AddComponentMenu("Anjin: Events/Trigger Events")]
	public class TriggerEvents : SerializedMonoBehaviour
	{
		[Title("Events")]
		[NonSerialized, ShowInPlay, PropertyOrder(3)] public CollisionEvent onTriggerEnter;
		[NonSerialized, ShowInPlay, PropertyOrder(3)] public CollisionEvent onTriggerStay;
		[NonSerialized, ShowInPlay, PropertyOrder(3)] public CollisionEvent onTriggerExit;

		public delegate void CollisionEvent(Collider other);

		private void Start()
		{
			Collider c = GetComponent<Collider>();

			if (!c)
			{
				Debug.LogError("The object has a TriggerEvents component but no trigger collider.", gameObject);
				return;
			}

			if (!c.isTrigger)
			{
				Debug.LogWarning("The object with a TriggerEvents component has a collider component, but it is not set to 'trigger'. The collider will be set to 'trigger' for convenience.", gameObject);
				c.isTrigger = true;
			}
		}

		private void OnTriggerEnter(Collider other)
		{
			onTriggerEnter?.Invoke(other);
		}

		private void OnTriggerStay(Collider other)
		{
			onTriggerStay?.Invoke(other);
		}

		private void OnTriggerExit(Collider other)
		{
			onTriggerExit?.Invoke(other);
		}
	}
}