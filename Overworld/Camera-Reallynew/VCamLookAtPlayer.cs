using System;
using Cinemachine;
using UnityEngine;

namespace Anjin.Cameras
{
	public class VCamLookAtPlayer : MonoBehaviour
	{
		private CinemachineVirtualCamera _virtualCamera;

		private void Start()  => _virtualCamera = GetComponent<CinemachineVirtualCamera>();
		private void Update() => _virtualCamera.LookAt = GameCams.Live.playerTarget.transform;
	}
}