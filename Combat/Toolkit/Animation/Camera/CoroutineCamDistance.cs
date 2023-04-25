namespace Combat.Toolkit.Camera
{
	public class CoroutineCamDistance : CoroutineCamFloatAnim
	{
		protected override float GetCurrentValue() => cam.Orbit.Coordinates.distance;

		protected override void ApplyChange(float chg)
		{
			cam.Orbit.Coordinates.distance += chg;
		}
	}
}