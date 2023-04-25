using UnityEngine.ResourceManagement.AsyncOperations;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class WaitableAsyncOperationHandle : ICoroutineWaitable
	{
		public AsyncOperationHandle handle;

		public WaitableAsyncOperationHandle(AsyncOperationHandle handle) 	=> this.handle = handle;
		public virtual bool CanContinue(bool                     justYielded, bool isCatchup) => handle.IsDone;
	}
}