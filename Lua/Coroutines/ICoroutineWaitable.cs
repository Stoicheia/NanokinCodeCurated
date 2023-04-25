namespace Anjin.Scripting
{
	/// <summary>
	/// An arbitrary object that can lock a CoroutineInstance until it says it's done.
	/// This is an interface so we can shim existing types into the coroutine system, as well as reducing
	/// allocation very slightly. (Probably not gonna be done too often, there is more power and flexibility
	/// with dedicated waitables wrapping game functionalities)
	/// </summary>
	public interface ICoroutineWaitable
	{
		bool CanContinue( bool justYielded, bool isCatchup = false);
	}
}