using KinematicCharacterController;
using UnityEngine;

namespace Anjin.Nanokin.Map
{
	public class PhysicsMoverProxy : MonoBehaviour, IMoverController
	{
		public PhysicsMover Mover;

		public Vector3 RotOffset;

		public void Awake()
		{
			if(Mover == null)
				Mover = GetComponent<PhysicsMover>();
			Mover.MoverController = this;
		}

		public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
		{
			goalPosition = transform.position;
			goalRotation = transform.rotation * Quaternion.Euler(RotOffset);
		}
	}
}