using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using UnityEngine;

namespace Util.Components.Cinemachine
{
	public interface ICameraStateUpdater
	{
		void UpdateState(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime);
	}
}
