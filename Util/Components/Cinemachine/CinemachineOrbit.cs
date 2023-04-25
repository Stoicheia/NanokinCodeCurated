using System;
using System.Collections.Generic;
using Anjin.Util;
using Cinemachine;
using MTAssets.UltimateLODSystem;
using UnityEngine;
using UnityEngine.Serialization;

namespace Util.Components.Cinemachine
{
	/// <summary>
	/// Allows the camera to position itself around a follow transform with spherical coordinates. (Azimuth, elevation and distance)
	/// </summary>
	[ExecuteInEditMode]
	public class CinemachineOrbit : CinemachineExtension
	{
		[FormerlySerializedAs("coordinates")]
		public SphereCoordinate Coordinates = new SphereCoordinate(45, 5, 5);

		[FormerlySerializedAs("_baseCoordinates"),SerializeField]
		public SphereCoordinate BaseCoordinates;

		public Vector3 Orientation;

		[NonSerialized] public bool invertAzimuth;

		private List<AdditiveOrbit> _additiveOrbits = new List<AdditiveOrbit>();
		private DebugData           _debugData;

		public SphereCoordinate FinalCoordinate
		{
			get
			{
				SphereCoordinate sum = BaseCoordinates;

				SphereCoordinate offset                      = Coordinates;
				if (invertAzimuth) offset.azimuth            =  -offset.azimuth;
				if (transform.parent != null) offset.azimuth += transform.parent.eulerAngles.y;
				sum += offset;

				for (int i = 0; i < _additiveOrbits.Count; i++)
				{
					AdditiveOrbit orbit = _additiveOrbits[i];

					if (orbit.Expired)
					{
						_additiveOrbits.RemoveAt(i--);
					}
					else
					{
						sum += orbit.coordinate;
					}
				}

				return sum;
			}
		}

		/// <summary>
		/// Computes the calculated final position of the camera, after damping.
		/// </summary>
		public Vector3 GetCorrection(SphereCoordinate coordinate)
		{
			Vector3 pos = Vector3.zero;

			pos += GetAzimuthOffset(coordinate.azimuth);
			pos *= coordinate.distance;
			pos += coordinate.elevation * Vector3.up;

			return pos;
		}

		private Vector3 GetAzimuthOffset(float azimuth)
		{
			// Azimuth(0) = (0, y, -1)
			// The camera is positioned at -Z when azimuth=0 such that it will face the object towards +Z.

			float theta = azimuth * Mathf.Deg2Rad;
			float sin   = Mathf.Sin(theta); // Sin(0) = 0
			float cos   = Mathf.Cos(theta); // Cos(0) = 1

			return new Vector3(-sin, 0, -cos);
		}

		public void AddAdditiveOrbit(AdditiveOrbit orbit)
		{
			_additiveOrbits.Add(orbit);
		}

		protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
		{
			if (stage != CinemachineCore.Stage.Body)
				return;

			// Apply damping smoothly towards target values.
			// if (Input.GetKey(KeyCode.Alpha9)) coordinates.azimuth -= 60f * Time.deltaTime;
			// if (Input.GetKey(KeyCode.Alpha0)) coordinates.azimuth += 60f * Time.deltaTime;

			SphereCoordinate finalCoordinate      = FinalCoordinate;
			Vector3          coordinateCorrection = GetCorrection(finalCoordinate);

			state.PositionCorrection += coordinateCorrection;

			state.OrientationCorrection = Quaternion.Euler(Orientation);

#if UNITY_EDITOR
			_debugData.exists            = true;
			_debugData.followPosition    = vcam.Follow.position;
			_debugData.correctedPosition = state.CorrectedPosition;
			_debugData.finalCoordinates  = finalCoordinate;
#endif
		}

		private void OnDrawGizmosSelected()
		{
			if (!_debugData.exists) return;

			Vector3 azimuthOffset = GetAzimuthOffset(_debugData.finalCoordinates.azimuth);

			Draw2.DrawLine(_debugData.followPosition, _debugData.correctedPosition, Color.green.Lerp(Color.black, 0.3f));
			Draw2.DrawLine(_debugData.followPosition, _debugData.followPosition - azimuthOffset * 2, Color.green);
		}

		private struct DebugData
		{
			public bool             exists;
			public Vector3          followPosition;
			public Vector3          correctedPosition;
			public SphereCoordinate finalCoordinates;
		}
	}
}