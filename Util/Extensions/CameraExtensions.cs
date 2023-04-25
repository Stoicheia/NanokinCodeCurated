using Anjin.Util;
using Cinemachine;
using Cinemachine.Utility;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Util.Extensions
{
	public static class CameraExtensions
	{
		public static bool ContainsPointSafe(this Camera cam, Vector2 pos)
		{
			if (cam == null)
				return false;

			Vector3 lo = cam.ViewportToScreenPoint(Vector3.zero);
			Vector3 hi = cam.ViewportToScreenPoint(Vector3.one);

			var screenrect = new Rect(lo, hi - lo);

			return screenrect.Contains(pos);
		}

		// NOTE (C.L. 02-8-2023): Copied from PlayerCamera.cs
		public static void ReorientHorizontal(this CinemachineVirtualCamera vcam, Vector3 facing)
		{
			vcam.UpdateCameraState(Vector3.up, 5);

			var orbital = vcam.GetCinemachineComponent<CinemachineOrbitalTransposer>();
			if (orbital == null) return;

			Vector3 current = vcam.transform.forward.Horizontal().normalized;
			Vector3 target  = facing.Horizontal().normalized;

			float diff = UnityVectorExtensions.SignedAngle(current, target, Vector3.up);

			AxisState haxis = orbital.m_XAxis;
			haxis.Value     += diff;
			orbital.m_XAxis =  haxis;

			vcam.UpdateCameraState(Vector3.up, 5);
		}
	}
}