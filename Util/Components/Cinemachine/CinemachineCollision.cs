using Anjin.Cameras;
using Anjin.Nanokin;
using Anjin.Util;
using Cinemachine;
using Drawing;
using KinematicCharacterController;
using UnityEngine;

namespace Util.Components.Cinemachine
{
	// <summary>
	/// An add-on module for Cinemachine Virtual Camera that post-processes
	/// the final position of the virtual camera. Pushes the camera out of intersecting colliders.
	/// https://forum.unity.com/threads/cinemachine-camera-collision.515626/#post-3460409
	/// </summary>
	[ExecuteInEditMode]
	[SaveDuringPlay]
	public class CinemachineCollision : CinemachineExtension
	{
		/// <summary>The Unity layer mask against which the collider will raycast.</summary>
		[Tooltip("The Unity layer mask against which the collider will raycast")]
		public LayerMask CollisionLayer = 1;
		public Vector3 MinDistanceOffset;
		public Vector3 MinDistanceLookOffset;
		public Vector3 TargetCastOffset;
		public float   SphereCastRadius = 0.15f;
		public Vector3 SlopeCorrection;

		public Vector3 ExternalOffsetModifier;
		public float AnySmoothing = 0.5f;

		private Transform _cachedLookTarget;
		private Transform _cachedFollowTarget;
		private KinematicCharacterMotor _kinematicCharacter;
		private Vector3 _trueSlopeModifier;
		private Ray        _ray;
		private RaycastHit _hit;

		private Vector3 RespectCameraTargetRay(Vector3 cameraPos, Vector3 targetPos)
		{
			var target = targetPos + TargetCastOffset;

			_ray.origin    = target;
			_ray.direction = cameraPos - target;

			//if (Physics.Raycast(_ray, out _hit, (cameraPos - target).magnitude, CollisionLayer.value, QueryTriggerInteraction.Ignore)) {
			if (Physics.SphereCast(_ray, SphereCastRadius, out _hit, (cameraPos - target).magnitude, CollisionLayer.value, QueryTriggerInteraction.Ignore)) {

				Vector3 point = _ray.origin + _ray.direction * _hit.distance;

				if(GameController.DebugMode) {
					//Debug.DrawLine(target, _hit.point, Color.red);
					//Debug.DrawLine(target, _hit.point, Color.red);
					Draw.editor.WireSphere(point - cameraPos, SphereCastRadius, Color.red);
				}

				if(!_hit.collider.HasComponent<IgnoreCameraCollision>())
					return point - cameraPos - TargetCastOffset;
			}

			return Vector3.zero;

		}

		protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
		{
			if (_cachedLookTarget != vcam.LookAt)
			{
				_cachedLookTarget = vcam.LookAt;
				_kinematicCharacter = _cachedLookTarget.GetComponent<KinematicCharacterMotor>();
			}

			if (_cachedFollowTarget != vcam.Follow) //currently redundant
			{
				_cachedFollowTarget = vcam.Follow;
			}

			// Move the body before the Aim is calculated
			if (stage == CinemachineCore.Stage.Body && vcam.LookAt != null)
			{
				//Vector3 displacement = RespectCameraRadius(state.RawPosition);
				//state.PositionCorrection += displacement;
				Vector3 referenceAxis = vcam ? vcam.transform.right : Vector3.right;
				float slope = _kinematicCharacter ? _kinematicCharacter.SignedSlope(referenceAxis) : 0;
				Vector3 slopeOffsetModifier = slope * SlopeCorrection;

				var displacement = RespectCameraTargetRay(state.RawPosition, _cachedLookTarget.position);
				state.PositionCorrection += displacement;
				_trueSlopeModifier = Vector3.Lerp(_trueSlopeModifier, slopeOffsetModifier, AnySmoothing);
				state.PositionCorrection += _trueSlopeModifier;

				if(state.HasLookAt && vcam.Follow) {
					var distance = Vector3.Distance(state.RawPosition, _cachedFollowTarget.position);
					/*var distance      = Vector3.Distance(state.RawPosition,    displacement);
					var totalDistance = Vector3.Distance(vcam.LookAt.position, state.RawPosition);*/

					var factor = displacement.magnitude / distance;
					state.PositionCorrection += Vector3.Lerp(Vector3.zero, MinDistanceOffset, factor);
					state.ReferenceLookAt = Vector3.Lerp(state.ReferenceLookAt, _cachedLookTarget.position + MinDistanceLookOffset, factor);
				}
			}
		}

	}
}