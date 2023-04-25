namespace Util {

	public interface IPool {

	}

	public interface IPool<TPoolee> : IPool
		where TPoolee : class
	{
		TPoolee Rent();
		void    Return(TPoolee thing);
	}

	public static class IPoolExtensions {



	}
}