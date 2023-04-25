namespace Combat.Toolkit.Camera
{
	public class CoroutineCamAzimuth : CoroutineCamFloatAnim
	{
		protected override float GetCurrentValue() => cam.Orbit.Coordinates.azimuth;

		protected override void ApplyChange(float chg)
		{
			cam.Orbit.Coordinates.azimuth += chg;
		}
	}
}