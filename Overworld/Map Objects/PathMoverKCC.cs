using KinematicCharacterController;
using UnityEngine;

namespace Anjin.Nanokin.Map
{
	public class PathMoverKCC : PathMover, IMoverController
	{
		public PhysicsMover Mover;

		private Vector3    targetPos;
		private Quaternion targetRot;

		protected override void Awake()
		{
			base.Awake();
			if (Mover == null)
				Mover = GetComponent<PhysicsMover>();
			Mover.MoverController = this;

			targetRot = transform.rotation;
		}

		void Start()
		{
			if (Type != MoverType.Train && GetSplineSample(out var pos, out var rot))
				Mover.SetPositionAndRotation(pos, rot);
		}

		public override void OnMove()
		{
			if (Type != MoverType.Train && GetSplineSample(out var pos, out var rot))
			{
				targetPos = pos;
				if(UsePathRotation)
					targetRot = rot;
			}
			else
			{
				targetPos = transform.position;
				targetRot = transform.rotation;
			}
		}

		public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
		{
			goalPosition = targetPos;
			goalRotation = targetRot;
		}
	}
}