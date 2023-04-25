using System;
using Anjin.Nanokin.Map;
using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;

namespace Anjin.Actors
{
	public class SlopeState : StateKCC
	{
		private float _elapsedTime;

		private Settings _settings;

		public SlopeState(Settings settings)
		{
			_settings = settings;
		}

		public bool ShouldSlide
		{
			get
			{
				if (Motor.GroundingStatus.GroundCollider == null)
					return false;

				float slopeAngle = SlopeAngle;

				float distanceToCapsuleBottom = Vector3.Distance(Motor.GroundingStatus.GroundPoint, Motor.transform.position + Motor.CharacterTransformToCapsuleBottom);
				if (distanceToCapsuleBottom > 0.12f)
					return false;

				if (Surface.all.TryGetValue(Motor.GroundingStatus.GroundCollider.gameObject.GetInstanceID(), out Surface slope))
				{
					if (slope != null && slope.ForceStable)
						return false;
				}

				if (slopeAngle.Between(_settings.minSlopeAngle, _settings.maxSlopeAngle))
					return true;

				// SlopeTerrain slope = Motor.GroundingStatus.GroundCollider.GetComponent<SlopeTerrain>();
				// if (slope != null)
				// {
				// return slope.OverrideMaxAngle || slopeAngle > SlopeSlideSettings.minSlopeAngle && slopeAngle < SlopeSlideSettings.maxSlopeAngle);
				// }

				return false;
			}
		}

		public float SlopeAngle
		{
			get
			{
				Vector3 groundNormal = actor.GetGroundNormal(actor.velocity);
				float   slopeAngle   = Vector3.Angle(Motor.CharacterUp, groundNormal);

				return slopeAngle;
			}
		}

		public override void UpdateVelocity(ref Vector3 currentVelocity, float deltaTime)
		{
			Debug.DrawLine(actor.transform.position, actor.transform.position + Motor.GroundingStatus.GroundNormal, Color.magenta);

			Vector3 groundNormal = actor.GetGroundNormal(currentVelocity);

			Transform transform = actor.transform;

			float slopeAngle = Vector3.Angle(Motor.CharacterUp, Motor.GroundingStatus.GroundNormal);
			float speed      = _settings.GetSlideSpeed(slopeAngle, _elapsedTime);

			//Get the direction of the slope projected on a plane facing up.
			Vector3 slopeDir = Math3d.ProjectVectorOnPlane(Vector3.up, Motor.GroundingStatus.GroundNormal).normalized;

			//Get the current planar movement direction based on the character up
			Vector3 planarVelocityDir = Math3d.ProjectVectorOnPlane(Motor.CharacterUp, currentVelocity).normalized;

			float diff = Quaternion.Angle(
				Quaternion.LookRotation(slopeDir, Vector3.up),
				Quaternion.LookRotation(planarVelocityDir, Vector3.up));

			// Are we moving up or down the slope?
			if (diff > 90 && currentVelocity.magnitude >= 0.5f)
			{
				// If we're moving up, we need to reverse direction until we start sliding down
				// targetDir = Vector3.Lerp(planarVelocityDir, slopeDir, 0.1f);

				currentVelocity = Vector3.Lerp(currentVelocity, Vector3.zero, 0.1f);
			}
			else
			{
				Quaternion slopeRot        = Quaternion.LookRotation(slopeDir, Vector3.up);
				Vector3    inputReoriented = (Quaternion.Inverse(slopeRot) * inputs.move).normalized;
				Quaternion rot             = Quaternion.Euler(0, 70 * inputReoriented.x, 0);
				Vector3    targetDir       = Vector3.Slerp(slopeDir, rot * slopeDir, 0.4f);

				//Project the target direction down the slope
				Vector3 targetDirProjected = Motor.GetDirectionTangentToSurface(targetDir, groundNormal);

				DebugDraw.DrawVector(transform.position, targetDir, 3, 0, Color.red, 0, false);
				DebugDraw.DrawVector(transform.position, targetDirProjected, 3, 0, Color.blue, 0, false);

				// Smooth our movement in the corrected target direction
				MathUtil.LerpWithSharpness(ref currentVelocity, targetDirProjected * speed, _settings.movementSharpness, deltaTime);
			}

			_elapsedTime += deltaTime;
		}

		[Serializable]
		public class Settings
		{
			public                      float          minSlopeAngle = 40;     // If a surface is below the min slope angle, it should be treated as ground.
			public                      float          maxSlopeAngle = 70;     // If a surface is above the max slope angle, it should just be treated as non-standable.
			public                      AnimationCurve slopeAngleSpeedScaling; // How any speed values scale from the minimum to maximum slope angles (0-1).
			public                      float          minSpeed = 2;           // The starting speed of sliding on a slope.
			public                      float          maxSpeed = 6;           // The maximum speed of sliding on a slope
			public                      AnimationCurve speedCurve;             // How the speed of sliding on a slope scales over time.
			[SuffixLabel("sec")] public float          speedupTime;            // How much time the speed curve takes to reach 1 on the x axis.
			public                      float          movementSharpness = 15f;

			public float GetSlideSpeed(float slopeAngle, float elapsedSlideTime)
			{
				float timeScale    = speedCurve.Evaluate(Mathf.Clamp01(elapsedSlideTime / speedupTime));
				float angleScaling = slopeAngleSpeedScaling.Evaluate(Mathf.Clamp01(slopeAngle / maxSlopeAngle));
				float spd          = minSpeed + (maxSpeed - minSpeed) * timeScale;

				return spd * angleScaling;
			}
		}
	}
}