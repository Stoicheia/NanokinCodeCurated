using Anjin.Nanokin;
using Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Anjin.Cameras
{
	/*
	 *	Intended as a way to package settings for different camera components into data structures.
	 *
	 *
	 */

	public interface CinemachineSettings<T>
	{
		void Apply(T com);
	}

	//public struct LensProxy : CinemachineProxy<LensSettings> { public void Apply(LensSettings com) { } }

	public struct OrbitalTransposerSettings : CinemachineSettings<CinemachineOrbitalTransposer>
	{
		public enum Mode { Input, Manual }

		public Mode  mode;
		public float Angle;
		//public AxisState XAxis;

		public void Apply(CinemachineOrbitalTransposer com)
		{
			if (com == null) return;

			AxisState haxis = com.m_XAxis;

			if (mode == Mode.Input)
			{
				if (GameCams.Live.InputAffectsCamera)
				{
					haxis.m_InputAxisName = "";
					haxis.m_InvertInput   = GameOptions.current.CameraPadInvertX;

					if (GameInputs.ActiveDevice != InputDevices.KeyboardAndMouse ||
						GameController.DebugMode && !Mouse.current.rightButton.isPressed)
					{
						haxis.m_InputAxisValue = GameInputs.look.Horizontal;
					}
					else
					{
						haxis.m_InputAxisValue =  0;
						haxis.Value            += GameInputs.GetCapturedMouseDelta().x / 4 * (GameOptions.current.CameraMouseInvertX ? -1 : 1);
					}
				}
				else
				{
					haxis.m_InputAxisValue = 0;
				}
			}
			else
			{
				haxis.m_InputAxisName = "";
				haxis.Value           = Angle;
			}

			com.m_XAxis = haxis;
		}

		public void SetAxisValue(float val)
		{
			mode  = Mode.Manual;
			Angle = val;

			//XAxis.Value = val;
		}

		public static OrbitalTransposerSettings Default = new OrbitalTransposerSettings
		{
			mode  = Mode.Input,
			Angle = 0,
			//XAxis = new AxisState(-180, 180, true, false, 300f, 0.1f, 0.1f, "Camera_Horizontal", true)
		};
	}

	public struct TransposerSettings : CinemachineSettings<CinemachineTransposer>
	{
		public bool 	RelativeOffset;
		public Vector3 	FollowOffset;
		public Vector3 	Damping;

		public void Apply(CinemachineTransposer com) => Apply(com, Quaternion.identity);
		public void Apply(CinemachineTransposer com, Quaternion parentRot)
		{
			if (com == null) return;

			com.m_FollowOffset = ( RelativeOffset ? parentRot : Quaternion.identity ) * FollowOffset;

			com.m_XDamping = Damping.x;
			com.m_YDamping = Damping.y;
			com.m_ZDamping = Damping.z;
		}
	}

	public struct ComposerSettings : CinemachineSettings<CinemachineComposer>
	{
		public Vector3 TrackedOffset;

		public void Apply(CinemachineComposer com)
		{
			if (com == null) return;

			com.m_TrackedObjectOffset = TrackedOffset;
		}
	}
}