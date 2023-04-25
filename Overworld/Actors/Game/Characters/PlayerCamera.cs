using System;
using System.Collections;
using Anjin.Cameras;
using Anjin.Nanokin;
using Anjin.Nanokin.Core.Options;
using Anjin.Scripting;
using Anjin.Util;
using Cinemachine;
using Cinemachine.Utility;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;
using Util.Components.Cinemachine;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	[LuaUserdata]
	[DefaultExecutionOrder(5)]
	public class PlayerCamera : MonoBehaviour
	{
		public bool  EnableLookInput      = true;
		public bool  EnableReorientInput  = true;
		public float ManualReorientSpeed  = 8;
		public float DefaultReorientSpeed = 8;

		[Title("Vertical")]
		[Range01]
		public FloatRange VertLookDeadzone = new FloatRange(0.1f, 0.5f);
		public float      VertDampingAccel  = 1.25f;
		public float      VertDampingDeccel = 3f;
		public FloatRange VertBodyOffsetY;
		public FloatRange VertBodyOffsetZ;
		public FloatRange VertAimOffsetY;
		public FloatRange VertAimOffsetZ;

		[NonSerialized] public CinemachineVirtualCamera vcam;
		[NonSerialized] public CinemachineZoom          zoom;
		[NonSerialized] public CinemachineTransposer    transposer;

		private bool                         _init;
		private CinemachineOrbitalTransposer _orbital;
		private CinemachineComposer          _composer;
		private Actor                        _player;
		private float                        _maxAxisSpeed;
		private Vector3?                     _reorientFacing;
		private float                        _reorientSpeed;
		private float                        _vertPos    = 0.5f; // 0 = closest, 1 = further away
		private float                        _vertTarget = 0.5f; // 0 = closest, 1 = further away
		//private bool                         _reorientInstant;

		[Title("Vertical Input")]
		public float VertPanSpeed = 0.5f;

		[Range(0, 1)]
		public float VertPanCooldown;

		private float _vertPanCenter;
		private float _vertPanMin;
		private float _vertPanMax;
		private float _scrollPos;
		private bool _canPanVertically;

		[Title("Scroll Input")]
		public float ScrollSpeed = 1.25f;

		private void Awake()
		{
			Init();
		}

		public void Init()
		{
			if (_init) return;
			_init      = true;
			transposer = GetComponent<CinemachineTransposer>();
			vcam       = GetComponent<CinemachineVirtualCamera>();
			zoom       = vcam.GetComponent<CinemachineZoom>();
			_orbital   = vcam.GetCinemachineComponent<CinemachineOrbitalTransposer>();
			//_orbital   = vcam.GetCinemachineComponent<CinemachineOrbitalTransposer>();
			_composer  = vcam.GetCinemachineComponent<CinemachineComposer>();

			_maxAxisSpeed = _orbital.m_XAxis.m_MaxSpeed;
			_scrollPos = _vertPos = 0.5f;
			_canPanVertically = false;
			StartCoroutine(EnableVertPanningSequence(VertPanCooldown));
		}


		public void SetCharacter([NotNull] Actor player)
		{
			vcam.Follow = player.transform;
			vcam.LookAt = player.transform;

			vcam.UpdateCameraState(Vector3.up, Time.deltaTime);

			_player = player as Actor;
		}

		private void Update()
		{
			if (vcam.Priority <= GameCams.PRIORITY_INACTIVE)
				return;

			vcam.m_Lens.FieldOfView = Quality.Current.FOV;

			AxisState haxis = _orbital.m_XAxis;
			haxis.m_InputAxisValue = 0;
			haxis.m_InputAxisName  = "";
			haxis.m_InvertInput    = GameOptions.current.CameraPadInvertX;
			haxis.m_MaxSpeed       = _maxAxisSpeed;

			int axislock = 0;

			if (_reorientFacing.HasValue)
			{
				Vector3 current = transform.forward.Horizontal().normalized;
				Vector3 target  = _reorientFacing.Value.Horizontal().normalized;

				float diff    = UnityVectorExtensions.SignedAngle(current, target, Vector3.up);
				float diffAbs = Mathf.Abs(diff);

				float change = diff * Time.deltaTime * _reorientSpeed;
				change = Mathf.Clamp(change, -diffAbs, diffAbs);

				haxis.Value += change;
				axislock    =  Math.Sign(change);

				if (diffAbs - change.Abs() < 2.5f)
					_reorientFacing = null;
			}

			float x = GameInputs.look.Horizontal;
			float y = GameInputs.look.Vertical;
			float z = GameInputs.scroll.Vertical;

			if (GameInputs.ActiveDevice == InputDevices.KeyboardAndMouse)
			{
				float prefsMultiplifer = 1 / Mathf.Max(GameOptions.current.CameraSensitivity, 0.001f) * (GameOptions.current.CameraMouseInvertX ? -1 : 1);
				x = GameInputs.GetCapturedMouseDelta().x * prefsMultiplifer;
				y = GameInputs.GetCapturedMouseDelta().y * prefsMultiplifer;
				z = GameInputs.GetCapturedScrollDelta() * prefsMultiplifer;
			}

			if (y.Abs() < VertLookDeadzone.Lerp(x.Abs()) || !_canPanVertically)
			{
				y = 0;
			}



			if (axislock == -1) x = Mathf.Min(x, 0);
			if (axislock == 1) x  = Mathf.Max(x, 0);


			// Inputs
			// ----------------------------------------

			bool enableInputs = EnableLookInput && GameCams.Live.InputAffectsCamera && _player != null && (_player.activeBrain == null || !_player.activeBrain.disableControls);
			if (enableInputs)
			{
				// Reorient
				// ----------------------------------------
				if (GameInputs.reorient.IsPressed)
				{
					Reorient(_player.facing, ManualReorientSpeed);
					_vertTarget = 0.5f;
				}

				// Look
				// ----------------------------------------

				switch (GameInputs.ActiveDevice)
				{
					case InputDevices.None:
						break;

					case InputDevices.Gamepad:
					{
						// This makes use of the haxis's acceleration and min/max feature
						haxis.m_InputAxisValue += x;

						_vertTarget = Mathf.Clamp01(_vertPos - y * Time.deltaTime * 4 * VertPanSpeed);
						_scrollPos = Mathf.Clamp01(_scrollPos + z * Time.deltaTime * ScrollSpeed / 100);
						break;
					}

					case InputDevices.KeyboardAndMouse:
					{
						// Directly add to the value so that the mouse is snappy
						_vertTarget = _vertPos = Mathf.Clamp01(_vertPos - y * Time.deltaTime * VertPanSpeed);
						_scrollPos = Mathf.Clamp01(_scrollPos + z * Time.deltaTime * ScrollSpeed / 100);

						haxis.Value            += x;

						haxis.m_InputAxisValue =  0;
						break;
					}
				}
			}

			_orbital.m_XAxis = haxis;

			if (!Mathf.Approximately(_vertPos, _vertTarget))
			{
				_vertPos = _vertPos.LerpDamp(_vertTarget, !Mathf.Approximately(y, 0) ? VertDampingAccel : VertDampingDeccel);
			}

			_orbital.m_FollowOffset.y         = VertBodyOffsetY.Lerp(_vertPos);
			_orbital.m_FollowOffset.z         = VertBodyOffsetZ.Lerp(_scrollPos);
			_composer.m_TrackedObjectOffset.y = VertAimOffsetY.Lerp(_vertPos);
			_composer.m_TrackedObjectOffset.z = VertAimOffsetZ.Lerp(_scrollPos);
		}

		public void Reorient(Vector3 playerFacing, float? speed = null)
		{
			_reorientFacing = playerFacing;
			_reorientSpeed  = speed ?? DefaultReorientSpeed;
		}

		public void ReorientInstant(Vector3 facing)
		{
			// This is required for the reorientation to work in some cases when the player moves
			// prior to this. Otherwise, the camera can automatically reorient to look towards it
			// after this function runs.
			//
			// For example, if we teleport the player perfectly behind this camera and then
			// immediately call ReorientInstant, the camera will be reoriented, but it is still
			// in front of the player, so instead of moving to a position that would preserve
			// this new facing, it will simply turn around 180-deg to look at the new position of
			// the player.
			vcam.UpdateCameraState(Vector3.up, 5);

			Vector3 current = transform.forward.Horizontal().normalized;
			Vector3 target  = facing.Horizontal().normalized;

			float diff = UnityVectorExtensions.SignedAngle(current, target, Vector3.up);

			AxisState haxis = _orbital.m_XAxis;
			haxis.Value      += diff;
			_orbital.m_XAxis =  haxis;

			vcam.UpdateCameraState(Vector3.up, 5);
		}

		IEnumerator EnableVertPanningSequence(float f)
		{
			yield return new WaitForSeconds(f);
			_canPanVertically = true;
		}
	}
}