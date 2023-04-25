using Cinemachine;

namespace Anjin.Actors
{
	/// <summary>
	/// An actor that simply controls a camera in some way.
	/// </summary>
	public class CameraActor : Actor
	{
		/// <summary>
		/// The camera this actor is controlling.
		/// </summary>
		public CinemachineVirtualCamera Camera;
	}
}