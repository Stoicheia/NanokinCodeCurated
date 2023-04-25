using Cinemachine;
using Sirenix.OdinInspector;

namespace Overworld
{
	public class VcamSetProjection : SerializedMonoBehaviour
	{
		[Button]
		public void SetOrtho()
		{
			var vcam = GetComponent<CinemachineVirtualCamera>();
			if (vcam)
				vcam.m_Lens.Orthographic = true;
		}

		[Button]
		public void SetPerspective()
		{
			var vcam = GetComponent<CinemachineVirtualCamera>();
			if (vcam)
				vcam.m_Lens.Orthographic = false;
		}

	}
}