using System;
using Anjin.Cameras;
using Cinemachine;
using UnityEngine;

namespace Anjin.Actors
{
	public class PlayerCameraRig : MonoBehaviour, ICamController
	{
		public PlayerCamera   AutoCam;
		public PlayerCamera   FreeCam;
		public AnimationCurve CameraBlend;

		[NonSerialized]
		public PlayerCamera activeCam;

		private Actor _actor;

		private void Start()
		{
			GameOptions.current.CameraAuto.AddHandler(OnOptionRefresh);
			refreshCameras();
		}

		private void OnDestroy()
		{
			GameOptions.current.CameraAuto.RemoveHandler(OnOptionRefresh);
		}

		private void OnOptionRefresh(bool newvalue) => refreshCameras();

		public void SetActor(Actor actor)
		{
			_actor = actor;
			FreeCam.SetCharacter(actor);
			AutoCam.SetCharacter(actor);

			ReorientInstant(actor.facing);
		}

		public void ReorientInstant(Vector3 facing)
		{
			FreeCam.ReorientInstant(facing);
			AutoCam.ReorientInstant(facing);
		}

		public void ReorientForwardInstant()
		{
			ReorientInstant(_actor.facing);
		}

		public void Reorient(Vector3 facing, float? speed = null)
		{
			FreeCam.Reorient(facing, speed);
			AutoCam.Reorient(facing, speed);
		}

		public void ReorientForward(float? speed = null)
		{
			Reorient(_actor.facing, speed);
		}

		private void refreshCameras()
		{
			FreeCam?.Init();
			AutoCam?.Init();

			if (GameOptions.current.CameraAuto)
			{
				activeCam = AutoCam;

				FreeCam.transform.position = AutoCam.transform.position;
				FreeCam.transform.rotation = AutoCam.transform.rotation;

				AutoCam.vcam.Priority = GameCams.PRIORITY_ACTIVE;
				FreeCam.vcam.Priority = GameCams.PRIORITY_INACTIVE;
			}
			else
			{
				activeCam = FreeCam;

				AutoCam.transform.position = FreeCam.transform.position;
				AutoCam.transform.rotation = FreeCam.transform.rotation;

				FreeCam.vcam.Priority = GameCams.PRIORITY_ACTIVE;
				AutoCam.vcam.Priority = GameCams.PRIORITY_INACTIVE;
			}
		}

		public void OnActivate() => refreshCameras();

		public void ActiveUpdate() { }

		public void OnRelease(ref CinemachineBlendDefinition? blend)
		{
			activeCam             = null;
			FreeCam.vcam.Priority = GameCams.PRIORITY_INACTIVE;
			AutoCam.vcam.Priority = GameCams.PRIORITY_INACTIVE;
		}

		public void GetBlends(ref CinemachineBlendDefinition? blend, ref CinemachineBlenderSettings settings)
		{
			blend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Custom, CameraBlend.keys[CameraBlend.length - 1].time)
			{
				m_CustomCurve = CameraBlend
			};
		}

		public void Teleport(Vector3 position, Vector3 facing)
		{
			AutoCam.vcam.ForceCameraPosition(position, Quaternion.LookRotation(facing));
			FreeCam.vcam.ForceCameraPosition(position, Quaternion.LookRotation(facing));
		}
	}
}