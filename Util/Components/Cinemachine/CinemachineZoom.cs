using Anjin.Util;
using Cinemachine;
using UnityEngine;
using Util.Odin.Attributes;

namespace Util.Components.Cinemachine
{
	[ExecuteInEditMode]
	public class CinemachineZoom : CinemachineExtension
	{
		public bool NormalizeDistance = false;

		[Range01] public float ZoomValue;
		[Range01] public float OrientationFix;

		[Space]
		public Transform towardsTransform;
		public Vector3? towardsPoint;

		public Vector3 StartPosition;

		protected override void OnEnable()
		{
			base.OnEnable();
			// _startPosition = transform.position;
		}

		protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
		{
			Vector3? target = null;

			if (towardsPoint.HasValue) target = towardsPoint;
			if (towardsTransform) target      = towardsTransform.position;

			float dist = NormalizeDistance && target.HasValue
				? Vector3.Distance(StartPosition, target.Value) * ZoomValue
				: ZoomValue;


			/*if(dist > 0 || ZoomValue > 0 || target != null)
				Debug.Log($"{dist} : {ZoomValue}, distance: {Vector3.Distance(StartPosition, target.Value)}, position correction: {state.PositionCorrection}, {state.PositionCorrection.magnitude}");*/

			if (target.HasValue)
			{
				state.PositionCorrection = state.CorrectedOrientation * (Vector3.forward * dist);

				var inv = Quaternion.Inverse(state.RawOrientation);                  // Cancel out the look orientation
				var dst = Quaternion.LookRotation(target.Value - state.RawPosition); // Rotate towards our zoom look at

				state.OrientationCorrection = Quaternion.Slerp(Quaternion.identity, inv * dst, OrientationFix);
			}
		}
	}
}