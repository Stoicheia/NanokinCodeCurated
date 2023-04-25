using System;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.Util;
using Core.Debug;
using ImGuiNET;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	public class FlyCameraActor : CameraActor
	{
		private const float JOYSTICK_DEADZONE = 0.01f;

		public float Zoom = 60;

		public float BaseSpeed   = 5f;
		public float MinSpeed    = 1f;
		public float MaxSpeed    = 25f;

		public float AdjustSpeed = 1.5f;
		public float FastScale   = 2f;

		public float TurnSpeed 			= 4f;
		public float TurnSpeedVScale 	= 1f;
		public float TurnSpeedHScale 	= 1f;

		public bool InvertTurnV = false;
		public bool InvertTurnH = false;

		public bool  Acceleration;
		public float AccelerationPerSecond = 0.5f;

		public float SpeedDamping    = 2f;
		public float RotationDamping = 2f;

		[DebugVars]
		[NonSerialized] public IFirstPersonFlightBrain OverrideBrain;
		[NonSerialized] public float hRotation = 0;
		[NonSerialized] public float vRotation = 0;
		[NonSerialized] public float baseSpeed = 0;

		[Space]
		private float _accel;
		private float   _speed;
		private float   _hRotation;
		private float   _vRotation;
		private Vector3 _moveDirection;

		protected override void Start()
		{
			base.Start();

			baseSpeed = BaseSpeed;
		}

		protected override void Update()
		{
			base.Update();

			FirstPersonFlightInputs inputs = FirstPersonFlightInputs.DefaultInputs;
			IFirstPersonFlightBrain brain  = activeBrain as IFirstPersonFlightBrain;

			if (brain == null)
				brain = OverrideBrain;

			if (brain == null)
			{
				Camera.Priority = -1;
			}
			else
			{
				Camera.Priority           = 1000;
				Camera.m_Lens.FieldOfView = Zoom;
				brain.PollInputs(ref inputs);
			}

			float inputMagnitude = inputs.MoveDirection.magnitude;

			// UPDATE ROTATION
			// ----------------------------------------
			if (inputs.Target != null)
			{
				// Snap rotation to the target object
			}
			else
			{
				hRotation += inputs.RotationDelta.x * (InvertTurnV ? -1 : 1) * TurnSpeed * TurnSpeedHScale;
				vRotation += inputs.RotationDelta.y * (InvertTurnH ? -1 : 1) * TurnSpeed * TurnSpeedVScale;

				// Prevent looking up past the top or bottom axis
				vRotation = Mathf.Clamp(vRotation, -90, 90);

				if (GameInputs.ActiveDevice == InputDevices.KeyboardAndMouse)
				{
					_hRotation = hRotation;
					_vRotation = vRotation;
				}
				else
				{
					_hRotation = _hRotation.LerpDamp(hRotation, RotationDamping);
					_vRotation = _vRotation.LerpDamp(vRotation, RotationDamping);
				}

				transform.rotation = Quaternion.Euler(_vRotation, _hRotation, 0);
			}

			Zoom = Mathf.Clamp(Zoom + inputs.ZoomDelta, 10f, 160f);

			// UPDATE SPEED
			// ----------------------------------------
			baseSpeed += inputs.SpeedDelta * AdjustSpeed;
			baseSpeed =  Mathf.Clamp(baseSpeed, MinSpeed, MaxSpeed);

			float targetSpeed = baseSpeed * (inputs.FastMode ? FastScale : 1) * inputMagnitude;

			if (Acceleration && inputMagnitude > JOYSTICK_DEADZONE)
			{
				_accel += AccelerationPerSecond * inputMagnitude * Time.unscaledDeltaTime;
				_accel =  Mathf.Clamp(_accel, 0, MaxSpeed);

				// Move faster than the target speed
				targetSpeed = Mathf.Max(targetSpeed, _accel);
			}
			else
			{
				// Move at least as fast as the current base speed
				_accel = baseSpeed;
			}

			_speed = _speed.LerpDamp(targetSpeed, SpeedDamping);

			// UPDATE POSITION
			// ----------------------------------------
			if (inputMagnitude > JOYSTICK_DEADZONE)
				_moveDirection = transform.rotation * inputs.MoveDirection.normalized;

			transform.position += _moveDirection * (_speed * Time.unscaledDeltaTime);
		}

		public void SetRotation(Vector3 euler)
		{
			vRotation = _vRotation = euler.x;
			hRotation = _hRotation = euler.y;
		}

		public void DrawImGUIControls(ref DebugSystem.State state)
		{
			ImGui.PushItemWidth(64);
			ImGui.TextColored(ColorsXNA.Goldenrod.ToV4(), "Move Speed");
			ImGui.InputFloat("Base", ref baseSpeed);
			ImGui.InputFloat("Min", ref baseSpeed);
			ImGui.InputFloat("Max", ref baseSpeed);
			ImGui.InputFloat("Fast Scale", ref FastScale);

			ImGui.Separator();
			ImGui.TextColored(ColorsXNA.Goldenrod.ToV4(), "Turn Speed");
			ImGui.InputFloat("Overall", ref TurnSpeed);
			ImGui.InputFloat("Horizontal Scale", ref TurnSpeedHScale);
			ImGui.InputFloat("Vertical Scale", ref TurnSpeedVScale);
			ImGui.PopItemWidth();

			ImGui.Checkbox("Invert X", ref InvertTurnV);
			ImGui.Checkbox("Invert Y", ref InvertTurnH);

			ImGui.Separator();
			ImGui.TextColored(ColorsXNA.Goldenrod.ToV4(), "Other");
			ImGui.InputFloat("Speed Damping", ref SpeedDamping);
			ImGui.InputFloat("Rotation Damping", ref RotationDamping);
			ImGui.Separator();
			ImGui.Checkbox("Acceleration", ref Acceleration);
			ImGui.PushItemWidth(64);
			ImGui.InputFloat("Per Second", ref AccelerationPerSecond);
			ImGui.PopItemWidth();

			ImGui.Separator();
			ImGui.TextColored(ColorsXNA.CornflowerBlue.ToV4(), "Position: "  + transform.position);
			ImGui.TextColored(ColorsXNA.CornflowerBlue.ToV4(), "Rotation: (" + hRotation + ", " + vRotation + ")");
			ImGui.TextColored(ColorsXNA.CornflowerBlue.ToV4(), "Accel: "     + _accel);
		}
	}
}