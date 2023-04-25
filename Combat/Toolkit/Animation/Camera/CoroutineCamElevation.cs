namespace Combat.Toolkit.Camera
{
	public class CoroutineCamElevation : CoroutineCamFloatAnim
	{
		protected override float GetCurrentValue() => cam.Orbit.Coordinates.elevation;

		protected override void ApplyChange(float chg)
		{
			cam.Orbit.Coordinates.elevation += chg;
		}
	}
}