using UnityEngine;

namespace Combat.Toolkit.Camera {
	public class CoroutineCamAim : CoroutineCamVector3Anim {

		protected override Vector3 GetCurrentValue() => cam.Orbit.Orientation;

		protected override void ApplyChange(Vector3 chg)
		{
			cam.Orbit.Orientation += chg;
		}
	}
}