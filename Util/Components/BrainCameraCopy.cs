using Anjin.Cameras;
using Sirenix.OdinInspector;

namespace Util
{
	public class BrainCameraCopy : SerializedMonoBehaviour
	{
		private TransformCopy _transformCopy;
		private CameraCopy    _cameraCopy;

		private void Start()
		{
			_cameraCopy    = gameObject.AddComponent<CameraCopy>();
			_transformCopy = gameObject.AddComponent<TransformCopy>();

			_cameraCopy.CopiedCamera       = GameCams.Live.UnityCam;
			_transformCopy.CopiedTransform = GameCams.Live.UnityCam.transform;
		}
	}
}