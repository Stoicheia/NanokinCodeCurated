using System;

namespace Util
{
	public class GenericPool<TPoolee> : BasePool<TPoolee>
		where TPoolee : class, new()
	{
		public GenericPool() : base(null) { }

		protected override TPoolee CreateNew(Action<TPoolee> createHandler)
		{
			TPoolee poolee = new TPoolee();
			createHandler?.Invoke(poolee);
			return poolee;
		}

		protected override string GetName() { return string.Empty; }

		protected override void SetName(TPoolee poolee, string name) { }

		protected override void Activate(TPoolee poolee) { }

		protected override void Deactivate(TPoolee poolee)
		{
			Recycle(poolee);
		}

		protected override void Deallocate(TPoolee poolee) { }
	}
}