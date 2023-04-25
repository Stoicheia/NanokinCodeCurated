namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class WaitableCoroutineInstance : ICoroutineWaitable
	{
		public CoroutineInstance Instance;

		public WaitableCoroutineInstance(CoroutineInstance instance) => Instance = instance;
		public virtual bool CanContinue(bool               justYielded, bool isCatchup) => !justYielded && Instance.Ended;

		public void cancel() => Instance?.Cancel();
	}
}