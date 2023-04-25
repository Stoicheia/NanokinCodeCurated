namespace Util
{
	/// <summary>
	/// A recyclable object which can be recycled for continuous reuse.
	///
	/// Note:
	/// we stray away from using the word 'Reset' because Unity already has its
	/// own Reset event function in MonoBehaviours and ScriptableObjects.....
	/// Which has a VERY contrived use-case..... unity.................
	///
	/// </summary>
	public interface IRecyclable
	{
		/// <summary>
		/// Recycle the object so its state is back to its initial
		/// base state and can be re-used.
		/// </summary>
		void Recycle();
	}
}