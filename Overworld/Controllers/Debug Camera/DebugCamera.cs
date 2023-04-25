using System;
using System.Collections.Generic;
using System.Text;
using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Nanokin;
using ImGuiNET;
using UnityEngine;
using UnityEngine.InputSystem;
using Util.Odin.Attributes;

namespace Overworld.Controllers
{
	public class DebugCamera : StaticBoy<DebugCamera>, IDebugDrawer, IFirstPersonFlightBrain
	{
		public bool Active => GameController.Live.ControlRoute == GameController.PlayerControlRoute.DebugCam;

		public FlyCameraActor DebugFlyCam;

		/*public enum DebugTool
		{
			None,
			TeleportPlayer,
		}*/

		[DebugVars]
		[NonSerialized]
		public bool showDebugCamToolbar;
		//[NonSerialized, ShowInInspector] public DebugTool CurrentDebugTool;

		[NonSerialized]
		public DebugCameraTool currentTool;

		[NonSerialized]
		public List<DebugCameraTool> tools;

		private StringBuilder _sb;

		public override void Awake()
		{
			base.Awake();
			DebugSystem.Register(this);
			showDebugCamToolbar = true;
			_sb                 = new StringBuilder();

			currentTool = null;
			tools       = new List<DebugCameraTool>();

			AddTool(new TeleportTool());
			AddTool(new ParkAITool());
			AddTool(new PartyAITool());

			void AddTool(DebugCameraTool tool)
			{
				tools.Add(tool);
			}
		}

		public void Activate()
		{
			DebugFlyCam.OverrideBrain = this;

			DebugFlyCam.transform.position = GameCams.Live.Brain.transform.position;
			DebugFlyCam.SetRotation(GameCams.Live.Brain.transform.rotation.eulerAngles);
		}

		public void Deactivate()
		{
			currentTool?.Exit();
			DebugFlyCam.OverrideBrain = null;
		}

		private void Update()
		{
			if (!Active) return;

			if (GameInputs.IsPressed(Key.F8) || Gamepad.current != null && Gamepad.current.selectButton.wasPressedThisFrame)
				showDebugCamToolbar = !showDebugCamToolbar;

			if (currentTool == null)
			{
				for (var i = 0; i < tools.Count; i++)
				{
					var tool = tools[i];
					if (tool.ShouldActivate())
					{
						currentTool = tool;
						tool.OnActivate();
						break;
					}
				}
			}
			else
			{
				currentTool.OnUpdate();
			}
		}

		public void OnLayout(ref DebugSystem.State state)
		{
			float bottom_tray_height = 32;

			if (Active)
			{
				if (showDebugCamToolbar)
				{
					if (ImGui.Begin("TestWind",
						ImGuiWindowFlags.NoTitleBar |
						ImGuiWindowFlags.NoResize |
						ImGuiWindowFlags.NoMove |
						ImGuiWindowFlags.NoScrollbar |
						ImGuiWindowFlags.NoSavedSettings |
						ImGuiWindowFlags.NoInputs |
						ImGuiWindowFlags.NoBringToFrontOnFocus))
					{
						ImGui.SetWindowPos(new Vector2(0,             Screen.height - bottom_tray_height));
						ImGui.SetWindowSize(new Vector2(Screen.width, bottom_tray_height));
						ImGui.SetCursorPos(new Vector2(4,             1));

						var isPad = GameInputs.ActiveDevice == InputDevices.Gamepad;
						var isKBM = GameInputs.ActiveDevice == InputDevices.KeyboardAndMouse && GameInputs.InputsEnabled;

						_sb.Clear();

						// First Row

						if (isKBM) _sb.Append("F8");
						if (isPad) _sb.Append("Select");
						_sb.Append(": Show/Hide  ");

						ImGui.PushStyleColor(ImGuiCol.Text, ColorsXNA.Goldenrod);
						ImGui.Text(_sb.ToString());
						ImGui.PopStyleColor();

						_sb.Clear();

						// Tools
						if (currentTool != null)
						{
							currentTool.OnToolbarString(_sb);

						}
						else
						{
							_sb.Append("Tools[ ");

							for (var i = 0; i < tools.Count; i++)
							{
								DebugCameraTool tool = tools[i];
								_sb.Append(tool.ShortcutString);
								_sb.Append(": ");
								_sb.Append(tool.Name);
								if (i < tools.Count - 1)
									_sb.Append(", ");
							}

							_sb.Append(" ]");
						}

						ImGui.SameLine();
						ImGui.PushStyleColor(ImGuiCol.Text, ColorsXNA.LightCoral);
						ImGui.Text(_sb.ToString());
						ImGui.PopStyleColor();

						_sb.Clear();


						// Second Row
						//_sb.Append("\n");
						_sb.Append("[WASD]/[LStick]: Move in X/Z axis, [Q/E]/[LTrigger/RTrigger]: Move in Y axis, [Mouse]/[RStick]: Look, [Scroll Wheel]/[DPad Up/ DPad Down]: FOV");

						ImGui.PushStyleColor(ImGuiCol.Text, ColorsXNA.Goldenrod);
						ImGui.Text(_sb.ToString());
						ImGui.PopStyleColor();
					}

					ImGui.End();

					if (currentTool != null)
					{
						currentTool.OnGui(ref state);
					}
				}

				if (GameController.DebugMode && ImGui.Begin("Debug Fly Camera"))
					DebugFlyCam.DrawImGUIControls(ref state);
				ImGui.End();
			}
		}

		public void PollInputs(ref FirstPersonFlightInputs inputs)
		{
			if (!Active)
			{
				inputs = FirstPersonFlightInputs.DefaultInputs;
				return;
			}

			inputs.MoveDirection = new Vector3(GameInputs.DebugCamMoveHor.Horizontal, GameInputs.DebugCamMoveVer.Value, GameInputs.DebugCamMoveHor.Vertical);

			float zoom       = 0;
			float zoom_scale = 1;
			if(GameInputs.ActiveDevice == InputDevices.KeyboardAndMouse) {
				zoom       = Mouse.current.scroll.y.ReadValue();
				zoom_scale = 5;
			} else {
				zoom = GameInputs.DebugCamMoveZoom.Value;
			}

			if(Mathf.Abs(zoom) > Mathf.Epsilon) {
				inputs.ZoomDelta = Mathf.Sign(zoom) * zoom_scale;
			}

			inputs.SpeedDelta    = GameInputs.DebugCamSpeedChange.Value;
			inputs.FastMode      = GameInputs.DebugCamFastMode.IsDown;

			if (GameInputs.ActiveDevice != InputDevices.KeyboardAndMouse ||
			    GameController.DebugMode && !Mouse.current.rightButton.isPressed)
			{
				inputs.RotationDelta = new Vector2(GameInputs.look.Horizontal, GameInputs.look.Vertical);
			}
			else
			{
				inputs.RotationDelta.x = GameInputs.GetCapturedMouseDelta().x / 60 / GameOptions.current.DebugCameraSensitivity * (GameOptions.current.CameraMouseInvertX ? -1 : 1);
				inputs.RotationDelta.y = GameInputs.GetCapturedMouseDelta().y / 60 / GameOptions.current.DebugCameraSensitivity * (GameOptions.current.CameraMouseInvertY ? -1 : 1);
			}
		}
	}
}