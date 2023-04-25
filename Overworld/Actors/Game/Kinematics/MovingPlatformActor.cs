using KinematicCharacterController;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Actors
{
	/// <summary>
	/// A basic moving platform.
	/// </summary>
	public class MovingPlatformActor : KinematicActor, IMoverController
	{
		public PhysicsMover Mover;
		public CharacterDetector detector;

		public Vector3 	TargetOffset;
		public float 	TimeToTarget;

		//public bool On = false;

		private Vector3 startingPosition;
		private Vector3 targetMovePosition;

		[ShowInInspector]
		private float currentLerpPos;

		protected override void Start()
		{
			base.Start();

			currentLerpPos = 0;
			targetMovePosition 	= transform.position;
			startingPosition 	= transform.position;

			Mover 	 = GetComponent<PhysicsMover>();
			detector = GetComponent<CharacterDetector>();

			if (Mover)
				Mover.MoverController = this;
		}

		public void UpdateMovement(out Vector3 goalPosition, out Quaternion goalRotation, float deltaTime)
		{
			float spd = TargetOffset.magnitude / TimeToTarget * deltaTime;

			if (detector)
			{
				if (detector.Detected)
				{
					if (currentLerpPos < 1)
					{
						currentLerpPos += spd;
					}
					else currentLerpPos = 1;
				}
				else
				{
					if (currentLerpPos > 0)
					{
						currentLerpPos -= spd;
					}
					else currentLerpPos = 0;
				}
			}

			targetMovePosition = Vector3.Lerp(startingPosition, startingPosition + TargetOffset, currentLerpPos);

			goalPosition = targetMovePosition;
			goalRotation = transform.rotation;
		}
	}
}