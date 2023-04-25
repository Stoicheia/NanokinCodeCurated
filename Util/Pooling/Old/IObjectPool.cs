namespace Util
{
	public interface IObjectPool<TObject>
	{
		/// <summary>
		/// Get an object from the pool.
		/// </summary>
		/// <returns></returns>
		TObject Get();

		/// <summary>
		/// Marks the pooled object as being locked.
		/// The object will be recycled when it is next released.
		/// If the object is not tarcked by this pool, an error should be raised.
		/// </summary>
		/// <param name="obj"></param>
		void LockPoolee(TObject obj);

		/// <summary>
		/// Release the pooled object. If the object is not tracked by this pool, an error should be raised.
		/// </summary>
		/// <param name="obj"></param>
		void ReleasePoolee(TObject obj);

		/// <summary>
		/// Get an object from the pool and lock it.
		/// </summary>
		/// <returns></returns>
		TObject GetAndLock();
	}
}