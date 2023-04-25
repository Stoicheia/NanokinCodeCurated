namespace Combat.Toolkit.Camera {
	public class CoroutineCamFov : CoroutineCamFloatAnim {

		protected override float GetCurrentValue() => cam.VCam.m_Lens.FieldOfView;

		protected override void ApplyChange(float chg)
		{
			cam.VCam.m_Lens.FieldOfView += chg;
		}
	}
}