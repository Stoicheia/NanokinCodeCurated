namespace Util
{
	public interface IObjectFactory<TObject>
	{
		/// <summary>
		/// Build an instance of the object provided by this IObjectFactory.
		/// </summary>
		/// <returns></returns>
		TObject BuildObject();
	}
}