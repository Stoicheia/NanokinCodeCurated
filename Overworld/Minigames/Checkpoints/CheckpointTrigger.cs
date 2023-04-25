using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace Anjin.Nanokin.Map
{
	public class CheckpointTrigger : SerializedMonoBehaviour
	{
		public float timeToReach;

		public UnityEvent Activated;
		public UnityEvent Deactivated;

		public event Action<Collider> TriggerEntered;

		private void OnTriggerEnter(Collider other)
		{
			TriggerEntered?.Invoke(other);
		}
	}
}