using Anjin.Actors;
using Cinemachine;

namespace Anjin.Cameras
{
	public struct CamController : ICamController
	{
		private readonly CinemachineVirtualCamera   _vcam;
		private readonly CinemachineBlendDefinition _enter;
		private readonly CinemachineBlendDefinition _leave;

		public CamController(CinemachineVirtualCamera vcam, CinemachineBlendDefinition enter, CinemachineBlendDefinition? leave = null)
		{
			_vcam  = vcam;
			_enter = enter;
			_leave = leave ?? enter;
		}

		public void OnActivate() { }

		public void OnRelease(ref CinemachineBlendDefinition? blend)
		{
			blend = _leave;
		}

		public void ActiveUpdate() { }

		public void GetBlends(ref CinemachineBlendDefinition? blend, ref CinemachineBlenderSettings settings)
		{
			blend = _enter;
		}
	}
}