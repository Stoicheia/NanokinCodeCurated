namespace Util.Components.Cinemachine
{
	public abstract class AdditiveOrbit
	{
		public SphereCoordinate coordinate;

		public abstract bool Expired { get; }
	}
}