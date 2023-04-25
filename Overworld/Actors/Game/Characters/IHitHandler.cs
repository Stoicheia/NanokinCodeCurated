namespace Anjin.Actors
{
	public interface IHitHandler<in TInfo> where TInfo:IHitInfo
	{
		void OnHit(TInfo hit);
		bool IsHittable(TInfo hit);
	}
}