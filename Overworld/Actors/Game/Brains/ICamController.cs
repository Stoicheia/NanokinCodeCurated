using Cinemachine;

namespace Anjin.Actors
{
	/// <summary>
	/// Specifies a class that can claim control of the in-game camera.
	/// </summary>
	public interface ICamController
	{
		/// <summary>
		/// When the controller is activated.
		/// </summary>
		void OnActivate();

		/// <summary>
		/// When the controller is released.
		/// </summary>
		/// <param name="blend">The blend that will be used when blending to the next controller.</param>
		void OnRelease(ref CinemachineBlendDefinition? blend);

		/// <summary>
		/// Called when the controller is active.
		/// </summary>
		void ActiveUpdate();

		/// <summary>
		/// Set the blends for the controller here.
		/// </summary>
		/// <param name="blend"></param>
		/// <param name="settings"></param>
		void GetBlends(ref CinemachineBlendDefinition? blend, ref CinemachineBlenderSettings settings);
	}
}