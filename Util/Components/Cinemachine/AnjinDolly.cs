using System;
using Anjin.Nanokin.Map;
using Anjin.Util;
using Cinemachine;
using UnityEngine;
#if UNITY_EDITOR
using Cinemachine.Editor;
using UnityEditor;
#endif

namespace Util.Components.Cinemachine {
	public class AnjinDolly : CinemachineComponentBase {

		[Flags]
		public enum RotationFlags {
			None = 0,
			x = 1,
			y = 2,
			z = 4,
		}

		// TODO: is it even possible to make this an IAnjinPathHolder reference?
		public AnjinPathComponent                Path;
		public float                             PathPosition;
		public CinemachinePathBase.PositionUnits Units;
		public RotationFlags                     RotFlags;
		public RotationFlags                     RotInvertFlags;
		public Vector3                           Offset;
		public Vector3                           RotOffset;

		public override void MutateCameraState(ref CameraState state, float dt)
		{
			if (dt < 0 || !VirtualCamera.PreviousStateIsValid)
			{
				_previousPathPosition   = PathPosition;
				_previousCameraPosition = state.RawPosition;
				_previousOrientation    = state.RawOrientation;
			}

			if (!IsValid) return;

			float correctedPathPosition = PathPosition;

			switch (Units) {
				case CinemachinePathBase.PositionUnits.PathUnits:  return; //TODO

				case CinemachinePathBase.PositionUnits.Normalized:
					correctedPathPosition = PathPosition * Path.Path.VertexPathLength;
					break;

				case CinemachinePathBase.PositionUnits.Distance:
					// Default
					break;
			}

			if (Path.Path.GetPositionAndRotationAtDistance(out Vector3 pos, out Quaternion rot, correctedPathPosition)) {
				// Position
				state.RawPosition    = pos;

				// Rotation
				Vector3 base_euler = state.RawOrientation.eulerAngles;
				Vector3 euler      = rot.eulerAngles;

				if ((RotFlags & RotationFlags.x) == RotationFlags.x)
					base_euler.x = (euler.x * ((RotInvertFlags & RotationFlags.x) == RotationFlags.x ? -1 : 1)) + RotOffset.x;

				if ((RotFlags & RotationFlags.y) == RotationFlags.y)
					base_euler.y = (euler.y * ((RotInvertFlags & RotationFlags.y) == RotationFlags.y ? -1 : 1)) + RotOffset.y;

				if ((RotFlags & RotationFlags.z) == RotationFlags.z)
					base_euler.z = (euler.z * ((RotInvertFlags & RotationFlags.z) == RotationFlags.z ? -1 : 1)) + RotOffset.z;

				state.RawOrientation = Quaternion.Euler(base_euler);
			}

			//float newPathPosition = PathPosition;

		}

		public override bool                  IsValid => enabled && Path != null && Path.Path != null;
		public override CinemachineCore.Stage Stage   => CinemachineCore.Stage.Body;

		private float      _previousPathPosition   = 0;
		private Vector3    _previousCameraPosition = Vector3.zero;
		private Quaternion _previousOrientation    = Quaternion.identity;

	}

	#if UNITY_EDITOR

	[CustomEditor(typeof(AnjinDolly))]
	public class AnjinDollyEditor : BaseEditor<AnjinDolly> {

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			//AnjinGUILayout.EnumToggleButtons(ref Target.RotFlags);
		}

	}

	#endif
}