using Cysharp.Threading.Tasks;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class WaitableUniTask : ICoroutineWaitable
	{
		private UniTask _uniTask;

		public WaitableUniTask(UniTask uniTask)
		{
			_uniTask = uniTask;
		}

		public virtual bool CanContinue(bool justYielded, bool isCatchup)
		{
			return _uniTask.Status != UniTaskStatus.Pending;
		}
	}
}