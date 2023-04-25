namespace Anjin.Utils
{
	public enum Motions
	{
		/// <summary>
		/// WARNING: This motion never reaches.
		/// </summary>
		None,

		/// <summary>
		/// WARNING: This motion never reaches.
		/// </summary>
		Prev,

		/// <summary>
		/// Use a CustomMotion component on the object.
		/// </summary>
		Custom,

		/// <summary>
		/// WARNING: This motion never reaches.
		/// </summary>
		Lock,

		/// <summary>
		/// Use a tween to move the object.
		/// </summary>
		Tween,

		/// <summary>
		/// Use accelerator physics and velocity.
		/// Good for rockets and stuff...
		/// </summary>
		Accelerator,

		/// <summary>
		/// Move with lerp damping formula.
		/// </summary>
		Damper,

		/// <summary>
		/// Move with Unity's SmoothDamping formula.
		/// </summary>
		SmoothDamp
	}
}