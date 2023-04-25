using KinematicCharacterController;
using UnityEngine;

namespace Anjin.Nanokin.Map
{
	public class TrainCarKCC : MonoBehaviour, IPathTrainCar, IMoverController
	{
		public PhysicsMover Mover;
		public Vector3 		PosOffset;
		public Vector3 		RotOffset;

		public Vector3 		TargetPos { get; set; }
		public Quaternion 	TargetRot { get; set; }

		void Awake()
		{
			Mover = GetComponent<PhysicsMover>();
			Mover.MoverController = this;
		}

		public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
		{
			goalPosition = TargetPos;
			goalRotation = TargetRot * Quaternion.Euler(RotOffset);
		}
	}
}