using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Anjin.Cameras;
using Anjin.Util;
using Core.Debug;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.Utilities;
using Util.Odin.Attributes;
using g = ImGuiNET.ImGui;
#if UNITY_EDITOR
using UnityEditor;

#endif

#if UNITY_EDITOR_WIN
using System.Text;
using Core.Input;
#endif

namespace Anjin.Nanokin
{
	public enum GamepadType
	{
		Xbox,
		PS,
		Switch,
		KeyboardMouse
	}

	public enum GamepadInput
	{
		buttonNorth,
		buttonSouth,
		buttonEast,
		buttonWest,
		dpad,
		leftShoulder,
		rightShoulder,
		leftTrigger,
		rightTrigger,
		leftStick,
		leftStickButton,
		rightStick,
		rightStickPress,
		start,
		select
	}

	public enum DesktopInput
	{
		q,
		w,
		e,
		r,
		t,
		y,
		u,
		i,
		o,
		p,
		a,
		s,
		d,
		f,
		g,
		h,
		j,
		k,
		l,
		z,
		x,
		c,
		v,
		b,
		n,
		m,
		keypad0,
		keypad1,
		keypad2,
		keypad3,
		keypad4,
		keypad5,
		keypad6,
		keypad7,
		keypad8,
		keypad9,
		leftButton,
		rightButton,
		middleButton,
		scroll,
		mouseMove,
		space,
		leftCtrl,
		rightCtrl,
		upArrow,
		downArrow,
		leftArrow,
		rightArrow,
		leftAlt,
		rightAlt,
		capsLock,
		delete,
		leftShift,
		rightShift,
		backspace,
		enter,
		tab,
		semicolon,
		comma,
		period,
		slash,
		backslash,
		leftBracket,
		rightBracket,
		minus,
		equals,
		backquote,
		quote
	}

	/// <summary>
	/// Main input system which does mouse capturing mainly..
	/// Previously it managed which device is currently being used, but we will have to
	/// review this feature since cInput has been changed to the new Unity3D input system.
	/// </summary>
	[DefaultExecutionOrder(-1000)]
	public class GameInputs : StaticBoy<GameInputs>, IDebugDrawer
	{
		private const float DEFAULT_HOLD_DELAY = 0.275f;

		/// <summary>
		/// A modular set of strings that can force the mouse to unlock the mouse.
		/// When this set has any content, the mouse is forced unlocked.
		/// Populate this externally to influence the behavior of the InputManager.
		/// </summary>
		[ShowInInspector, HideInEditorMode]
		public static HashSet<string> forceUnlocks = new HashSet<string>();

		/// <summary>
		/// A modular set of strings that can force the mouse to lock.
		/// When this set has any content, the mouse is forced to be lock.
		/// Populate this externally to influence the behavior of GameInputs.
		/// Overrides mouseUnlocks and forceUnlocks.
		/// </summary>
		[ShowInInspector, HideInEditorMode]
		public static HashSet<string> forceLocks = new HashSet<string>();

		/// <summary>
		/// Like forceUnlocks, but only for KeyboardAndMouse device.
		/// </summary>
		[ShowInInspector, HideInEditorMode]
		public static HashSet<string> mouseUnlocks = new HashSet<string>();

		/// <summary>
		/// Disable all game inputs.
		/// </summary>
		[ShowInInspector, HideInEditorMode]
		public static HashSet<string> inputDisables = new HashSet<string>();

		public static List<IHasInputAction> AllInputs = new List<IHasInputAction>();

		private InputIcons inputIcons;

		// Joysticks
		public static Joystick move;
		public static Joystick look;
		public static Joystick scroll;
		public static Joystick scrollWheel;

		// Buttons
		public static ActionButton confirm;
		public static ActionButton cancel;
		public static ActionButton menu;
		public static ActionButton menuLeft;
		public static ActionButton menuRight;
		public static ActionButton menuLeft2;  // (usually left trigger)
		public static ActionButton menuRight2; // (usually right trigger)
		public static Joystick     menuNavigate;
		public static ActionButton detachItem;
		public static ActionButton favoriteItem;
		public static ActionButton toggleMode;
		public static ActionButton showOverworldHUD;
		public static ActionButton partyChat;
		public static ActionButton textBoxSoftAuto;
		public static ActionButton selectField;

		// ---
		public static ActionButton jump;
		public static ActionButton run;
		public static ActionButton pogo;
		public static ActionButton dive;
		public static ActionButton sword;
		public static ActionButton interact;
		public static ActionButton reorient;

		public static ActionButton splicer;

		// ---
		public static ActionButton overdriveDown;
		public static ActionButton overdriveUp;
		public static ActionButton showInfo;

		// Quick shortcuts
		public static ActionButton hold;
		public static ActionButton flee;
		public static ActionButton skill1, skill2, skill3;

		//Debug
		public static Joystick     DebugCamMoveHor;
		public static AnalogAxis   DebugCamMoveVer;
		public static AnalogAxis   DebugCamMoveZoom;
		public static AnalogAxis   DebugCamSpeedChange;
		public static ActionButton DebugCamFastMode;

		[ShowInInspector]
		public static Dictionary<string, IHasInputAction> FieldNamesToControls = new Dictionary<string, IHasInputAction>();

		[NonSerialized] public Inputs inputs;

		[Title("Debug")]
		[ShowInPlay, ReadOnly] private InputDevices _activeDevice, _lastRealDevice;
		[ShowInPlay, ReadOnly] private bool _escaped;
		[ShowInPlay, ReadOnly] private bool _inputState;
		[ShowInPlay, ReadOnly] private bool _prevFocused;

		public static bool InputsEnabled => GetLockState() > LockState.None;
		public static bool MouseEnabled  => GetLockState() == LockState.Full;

		public static bool DebugDown    => DualShockGamepad.current?.touchpadButton.isPressed == true || Gamepad.current?.startButton.isPressed == true;
		public static bool DebugPressed => DualShockGamepad.current?.touchpadButton.wasPressedThisFrame == true || Gamepad.current?.startButton.wasPressedThisFrame == true;

		public static event Action<InputDevices> DeviceChanged;

		public const string GROUP_KEYBOARD = "KeyboardMouse";
		public const string GROUP_GAMEPAD  = "Gamepad";

		[ShowInPlay]
		public GamepadType CurrentController { get; private set; }

		public GamepadType LastGamepadUsed { get; private set; }

		public readonly static char[] PathDelimeter = new char[] { '/' };

		/// <summary>
		/// Set the devices to use for inputs.
		/// Currently doesn't do much because the new input system is limited.
		/// </summary>
		[ShowInPlay, ReadOnly]
		public static InputDevices ActiveDevice
		{
			get => Live._activeDevice;
			set
			{
#if UNITY_EDITOR
				// In the editor, it's useful to keep the gamepad working at all time
				// since we could be mucking around the inspectors and testing quickly
				if (value == InputDevices.None)
				{
					value = InputDevices.Gamepad;
				}
#endif
				bool hasChanged = Live._activeDevice != value;
				Live._activeDevice = value;

				//Debug.Log($"GameInputs: Switch to {Live._activeDevice} (Last real: {Live._lastRealDevice})");

				if (value != InputDevices.None)
					Live._lastRealDevice = value;

				look.withMouseDelta = false; /*= value == InputDevices.KeyboardAndMouse;*/
				switch (value)
				{
					case InputDevices.None:
						Live.inputs.bindingMask = null;
						break;

					case InputDevices.KeyboardAndMouse:
						Live.inputs.bindingMask = InputBinding.MaskByGroup(Live.inputs.KeyboardMouseScheme.bindingGroup);
						Live.CurrentController  = GamepadType.KeyboardMouse;
						break;

					case InputDevices.Gamepad:
						Live.inputs.bindingMask = InputBinding.MaskByGroup(Live.inputs.GamepadScheme.bindingGroup);

						Gamepad gamepad = Gamepad.current;

						bool isDualShock = (gamepad is DualShockGamepad);
						bool isXInput    = (gamepad is UnityEngine.InputSystem.XInput.XInputController);

						if (!isDualShock && !isXInput)
						{
							Live.CurrentController = GamepadType.Switch;
							Live.LastGamepadUsed   = GamepadType.Switch;
						}
						else if (isDualShock)
						{
							Live.CurrentController = GamepadType.PS;
							Live.LastGamepadUsed   = GamepadType.PS;
						}
						else
						{
							Live.CurrentController = GamepadType.Xbox;
							Live.LastGamepadUsed   = GamepadType.Xbox;
						}

						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(value), value, null);
				}


				if (hasChanged)
				{
					// Fixes a bug where clicking into the window would
					foreach (Button btn in Button.All)
					{
						btn.Mute(0.1f);
					}

					DeviceChanged?.Invoke(Live._activeDevice);
				}

				Live._lastRealDevice = ActiveDevice;
			}
		}

		public static InputDevices LastRealDevice => Live._lastRealDevice;

	#region Initialization

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			forceLocks.Clear();
			mouseUnlocks.Clear();
			forceUnlocks.Clear();
			AllInputs.Clear();
			FieldNamesToControls.Clear();

			// Joysticks
			move   = null;
			look   = null;
			scroll = null;

			// Buttons
			menu             = null;
			menuNavigate     = null;
			showOverworldHUD = null;
			partyChat        = null;
			textBoxSoftAuto  = null;
			interact         = null;
			reorient         = null;
			confirm          = null;
			cancel           = null;
			jump             = null;
			run              = null;
			pogo             = null;
			dive             = null;
			sword            = null;
			splicer          = null;
			overdriveDown    = null;
			overdriveUp      = null;
			showInfo         = null;
			hold             = null;
			flee             = null;
			skill1           = null;
			skill2           = null;
			skill3           = null;

			menuLeft     = null;
			menuRight    = null;
			menuLeft2    = null;
			menuRight2   = null;
			detachItem   = null;
			favoriteItem = null;
			toggleMode   = null;
			selectField  = null;
		}

		public static Sprite GetInputIcon(InputAction inputAction)
		{
			if(!Exists || Live.inputIcons == null) return null;

			Sprite icon = null;

			if (inputAction != null)
			{
				var bindings = inputAction.bindings;

				if (Live.CurrentController == GamepadType.KeyboardMouse)
				{
					int bindingIndex = inputAction.GetBindingIndex(group: "KeyboardMouse");

					if ((bindingIndex >= 0) && (bindingIndex < bindings.Count))
					{
						string[] tokens = bindings[bindingIndex].path.Split(PathDelimeter, StringSplitOptions.RemoveEmptyEntries);

						if (tokens.Length > 0)
						{
							string button = tokens[tokens.Length - 1];

							int keypadNumber;

							bool isIntKey = int.TryParse(button, out keypadNumber);

							if (isIntKey)
							{
								button = string.Format("keypad{0}", keypadNumber);
							}

							DesktopInput inputKey;
							bool         success = System.Enum.TryParse(button, out inputKey);

							if (success)
							{
								icon = Live.inputIcons.desktopIcons[inputKey];
							}
						}
					}
				}
				else
				{
					int bindingIndex = inputAction.GetBindingIndex(group: "Gamepad");

					if ((bindingIndex >= 0) && (bindingIndex < bindings.Count))
					{
						string[] tokens = bindings[bindingIndex].path.Split(PathDelimeter, System.StringSplitOptions.RemoveEmptyEntries);

						if (tokens.Length > 0)
						{
							string button = tokens[tokens.Length - 1];

							GamepadInput inputKey;
							bool         success = System.Enum.TryParse(button, out inputKey);

							if (success)
							{
								icon = Live.inputIcons.gamepadIcons[Live.CurrentController][inputKey];
							}
						}
					}
				}
			}

			return icon;
		}

		public static Sprite GetInputIconForPlatform(Overworld.UI.Settings.SettingsMenu.BindingInfo bindingInfo, GamepadType platform)
		{
			if (!Exists || Live.inputIcons == null) return null;

			Sprite icon = null;

			string mapping = bindingInfo.Mapping;

			switch (bindingInfo.Platform)
			{
				case "Keyboard":
					int keypadNumber;

					bool isIntKey = int.TryParse(mapping, out keypadNumber);

					if (isIntKey)
					{
						mapping = string.Format("keypad{0}", keypadNumber);
					}

					DesktopInput inputKey;
					bool         success = Enum.TryParse(mapping, out inputKey);

					if (success)
					{
						icon = Live.inputIcons.desktopIcons[inputKey];
					}

					break;
				case "Mouse":

					success = Enum.TryParse(mapping, out inputKey);

					if (success)
					{
						icon = Live.inputIcons.desktopIcons[inputKey];
					}

					break;
				case "Gamepad":

					GamepadInput buttonKey;
					success = System.Enum.TryParse(mapping, out buttonKey);

					if (success)
					{
						icon = Live.inputIcons.gamepadIcons[platform][buttonKey];
					}

					break;
				default:
					break;
			}

			return icon;
		}

		public static Sprite GetInputIconForPlatform(InputAction inputAction, GamepadType platform)
		{
			if (!Exists || Live.inputIcons == null) return null;

			Sprite icon = null;

			if (inputAction != null)
			{
				var bindings = inputAction.bindings;

				if (platform == GamepadType.KeyboardMouse)
				{
					int bindingIndex = inputAction.GetBindingIndex(group: "KeyboardMouse");

					if ((bindingIndex >= 0) && (bindingIndex < bindings.Count))
					{
						string[] tokens = bindings[bindingIndex].path.Split(PathDelimeter, System.StringSplitOptions.RemoveEmptyEntries);

						if (tokens.Length > 0)
						{
							string button = tokens[tokens.Length - 1];

							int keypadNumber;

							bool isIntKey = int.TryParse(button, out keypadNumber);

							if (isIntKey)
							{
								button = string.Format("keypad{0}", keypadNumber);
							}

							DesktopInput inputKey;
							bool         success = System.Enum.TryParse(button, out inputKey);

							if (success)
							{
								icon = Live.inputIcons.desktopIcons[inputKey];
							}
						}
					}
				}
				else
				{
					int bindingIndex = inputAction.GetBindingIndex(group: "Gamepad");

					if ((bindingIndex >= 0) && (bindingIndex < bindings.Count))
					{
						string[] tokens = bindings[bindingIndex].path.Split(PathDelimeter, System.StringSplitOptions.RemoveEmptyEntries);

						if (tokens.Length > 0)
						{
							string button = tokens[tokens.Length - 1];

							GamepadInput inputKey;
							bool         success = System.Enum.TryParse(button, out inputKey);

							if (success)
							{
								icon = Live.inputIcons.gamepadIcons[platform][inputKey];
							}
						}
					}
				}
			}

			return icon;
		}

		protected override void OnAwake()
		{
			Keyboard.current.SetIMEEnabled(false);

			// The 'Inputs' class is generated by the control asset
			// It's almost like NanokinInputs here.
			var i = new Inputs();
			inputs = i;

			// yeah the new input system is still rough around the edges, so we're still wrapping it with our own classes
			// since actions are not considered buttons, they don't have a way to just check if an action is down or not,
			// or whether it was just pressed for 1 frame, etc.. it would be obvious if they'd made a game with their shit
			confirm          = new ActionButton(i.General.Confirm);
			cancel           = new ActionButton(i.General.Cancel);
			menu             = new ActionButton(i.General.Menu);
			move             = new Joystick(i.General.Move);
			look             = new Joystick(i.General.Look);
			scroll           = new Joystick(i.General.Scroll);
			scrollWheel      = new Joystick(i.UI.ScrollWheel);
			interact         = new ActionButton(i.Overworld.Interact);
			reorient         = new ActionButton(i.Overworld.Reorient);
			jump             = new ActionButton(i.Overworld.Jump);
			run              = new ActionButton(i.Overworld.Run);
			pogo             = new ActionButton(i.Overworld.Pogo);
			dive             = new ActionButton(i.Overworld.Dive);
			sword            = new ActionButton(i.Overworld.Sword);
			splicer          = new ActionButton(i.Overworld.Splicer);
			showOverworldHUD = new ActionButton(i.Overworld.ShowOverworldHUD);
			partyChat        = new ActionButton(i.General.PartyChat);
			textBoxSoftAuto  = new ActionButton(i.General.TextboxSoftAuto);
			// --
			overdriveDown = new ActionButton(i.Battle.OverdriveDown);
			overdriveUp   = new ActionButton(i.Battle.OverdriveUp);
			showInfo      = new ActionButton(i.Battle.ShowInfo);
			hold          = new ActionButton(i.Battle.Hold);
			flee          = new ActionButton(i.Battle.Flee);
			skill1        = new ActionButton(i.Battle.Skill1);
			skill2        = new ActionButton(i.Battle.Skill2);
			skill3        = new ActionButton(i.Battle.Skill3);

			menuLeft     = new ActionButton(i.Menu.Left);
			menuRight    = new ActionButton(i.Menu.Right);
			menuLeft2    = new ActionButton(i.Menu.Left2);
			menuRight2   = new ActionButton(i.Menu.Right2);
			menuNavigate = new Joystick(i.UI.Navigate);
			detachItem   = new ActionButton(i.Menu.Detach);
			favoriteItem = new ActionButton(i.Menu.Favorite);
			toggleMode   = new ActionButton(i.Menu.Toggle);
			selectField  = new ActionButton(i.Menu.SelectField);

			DebugCamMoveHor     = new Joystick(i.Debug.MoveDebugCamHor);
			DebugCamMoveVer     = new AnalogAxis(i.Debug.MoveDebugCamVer);
			DebugCamMoveZoom    = new AnalogAxis(i.Debug.ZoomDebugCam);
			DebugCamFastMode    = new ActionButton(i.Debug.FastModeDebugCam);
			DebugCamSpeedChange = new AnalogAxis(i.Debug.SpeedDebugCam);

			ActiveDevice = InputDevices.None;

			RefreshFieldNames();

			LoadBindings();
		}

		[Button]
		private static void RefreshFieldNames()
		{

			FieldNamesToControls.Clear();

			Type type = typeof(GameInputs);

			FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);

			foreach (FieldInfo info in fields) {
				if (info.FieldType.InheritsFrom<IHasInputAction>()) {
					object val = info.GetValue(null);
					FieldNamesToControls[info.Name] = (IHasInputAction)val;
				}
			}
		}

		private void OnEnable()
		{
			DebugSystem.drawers.Add(this);
		}

		private void Start()
		{
			inputIcons = GameAssets.Live.InputIconsObject;

			DebugConsole.AddCommand("inputs", (digester, io) => io.output.Add(Live));

			// Setup the initial state
			// ----------------------------------------
#if !UNITY_EDITOR
			bool windowFocused = Application.isFocused;
#else
			bool windowFocused = Application.isFocused && EditorWindow.focusedWindow && EditorWindow.focusedWindow.GetType().Name.Contains("Game"); // soo fcking done with unity's shit
			_escaped = !(windowFocused && GameOptions.current.mouselock_on_startup);
#endif

			Application.focusChanged += OnApplicationFocusChanged;

			if (!_escaped)
			{
				ActiveDevice = InputDevices.KeyboardAndMouse;
				if (Gamepad.current != null)
					ActiveDevice = InputDevices.Gamepad;
			}
		}

	#endregion


		private static void OnApplicationFocusChanged(bool obj)
		{
			// Reset the state of buttons, otherwise they can stay stuck
			// if we can't catch the release event for any reason
			// This happens especially when using a debugger and hitting breakpoints
			foreach (Button button in Button.All) button.Reset();
			foreach (AnalogAxis axis in AnalogAxis.All) axis.Reset();
		}


	#region Checks

		public static bool IsPressed(Key key)
		{
			if (Keyboard.current == null) return false;
			if (!Application.isFocused) return false;
			if (!GetInputState()) return false;
			if (g.IsAnyItemActive() || g.IsAnyItemFocused()) return false;

			return Keyboard.current[key].wasPressedThisFrame;
		}

		public static bool IsPressed([CanBeNull] ButtonControl control)
		{
			if (control == null) return false;
			if (!InputsEnabled) return false;
			if (g.IsAnyItemActive() || g.IsAnyItemFocused()) return false;

			return control.wasPressedThisFrame;
		}

		public static bool IsDown(Key key)
		{
			if (Keyboard.current == null) return false;
			if (!Application.isFocused) return false;
			if (!GetInputState()) return false;

			return Keyboard.current[key].isPressed;
		}

		/// <summary>
		/// Function to add debug shortcuts.
		/// Should be used instead of accessing Keyboard.current directly,
		/// since it takes a few more things into account.
		/// </summary>
		public static bool IsShortcutPressed(Key key, Key modifier = Key.None)
		{
			// NOTE: LeftShift as a modifier doesn't seem to work in editor...... thanks unity. Maybe works in builds..
			if (Keyboard.current == null) return false;
			if (!Application.isFocused) return false;
			if (g.IsAnyItemActive()) return false;

			return Keyboard.current[key].wasPressedThisFrame && (modifier == Key.None || Keyboard.current[modifier].isPressed);
		}

		private static bool GetInputState()
		{
			if (ActiveDevice == InputDevices.None) return false;
			if (ActiveDevice == InputDevices.KeyboardAndMouse && (g.IsAnyItemFocused() || g.IsAnyItemActive() || g.IsAnyItemHovered())) return false;

			return inputDisables.Count == 0;
		}

		private static LockState GetLockState()
		{
			if (!Application.isFocused || Live._escaped)
				return LockState.None;

			if (forceLocks.Count > 0) return LockState.Full;
			if (forceUnlocks.Count > 0) return LockState.Partial;

			if (ActiveDevice == InputDevices.KeyboardAndMouse)
			{
				if (mouseUnlocks.Count > 0)
					return LockState.Partial;
			}

			return LockState.Full;
		}

	#endregion

		private void Update()
		{
			Mouse    mouse    = Mouse.current;
			Keyboard keyboard = Keyboard.current;

#if UNITY_EDITOR_LINUX || PLATFORM_STANDALONE_LINUX || UNITY_STANDALONE_LINUX
			// oxy: I am facing a bug where my gamepad results in a duplicate device with weird inputs...
			if (Gamepad.all.Count > 1)
			{
				for (var i = 0; i < Gamepad.all.Count; i++)
				{
					Gamepad gamepad = Gamepad.all[i];
					if (Math.Abs(gamepad.leftStick.y.ReadValue()) > 0.1f &&
					    Math.Abs(gamepad.leftTrigger.ReadValue()) > 0.3f &&
					    Math.Abs(gamepad.leftTrigger.ReadValue()) > 0.3f)
					{
						InputSystem.RemoveDevice(gamepad);
					}
				}
			}
#endif

//#if UNITY_EDITOR
			// MANUAL ESCAPING
			// ----------------------------------------

			if (_escaped && Application.isFocused && mouse?.leftButton.wasPressedThisFrame == true)
			{
				Vector3 view      = GameCams.Live.UnityCam.ScreenToViewportPoint(Input.mousePosition);
				bool    isOutside = view.x < 0 || view.x > 1 || view.y < 0 || view.y > 1;

				if (!isOutside)
					_escaped = false;
			}

			if (IsPressed(Key.Escape) || IsDown(Key.CapsLock))
			{
				_escaped = true;
				// FixEditorShortcutsOnWindows();
			}
//#endif

			// LOCK STATE
			// ----------------------------------------
			LockState lockState = GetLockState();

			// DEVICE DETECTION
			// ----------------------------------------
			if (lockState > LockState.None)
			{
				// Keyboard/Mouse when clicking
				if (mouse != null && ActiveDevice != InputDevices.KeyboardAndMouse)
				{
					if (mouse.leftButton.wasPressedThisFrame ||
					    mouse.rightButton.wasPressedThisFrame ||
					    mouse.middleButton.wasPressedThisFrame || Application.isFocused && !_prevFocused)
						ActiveDevice = InputDevices.KeyboardAndMouse;
				}

				if (keyboard != null && ActiveDevice != InputDevices.KeyboardAndMouse)
				{
					for (int i = (int)Key.A; i < (int)Key.Z; i++)
					{
						if (keyboard[(Key)i].wasPressedThisFrame)
						{
							ActiveDevice = InputDevices.KeyboardAndMouse;
						}
					}

					if (keyboard.spaceKey.wasPressedThisFrame ||
					    keyboard.leftCtrlKey.wasPressedThisFrame ||
					    keyboard.enterKey.wasPressedThisFrame)
						ActiveDevice = InputDevices.KeyboardAndMouse;
				}

				if (Gamepad.current != null && ActiveDevice != InputDevices.Gamepad)
				{
					for (var i = 0; i < Gamepad.current.allControls.Count; i++)
					{
						InputControl control = Gamepad.current.allControls[i];

						if (control is ButtonControl bc && bc.wasPressedThisFrame)
						{
							ActiveDevice = InputDevices.Gamepad;
							break;
						}

						if (control is UnityEngine.InputSystem.Joystick jc && jc.IsActuated(0.25f))
						{
							ActiveDevice = InputDevices.Gamepad;
							break;
						}
					}
				}
			}
			else if (ActiveDevice != InputDevices.None)
			{
				ActiveDevice = InputDevices.None;
			}

			// INPUT STATE UPDATE
			// ----------------------------------------
			bool inputState = GetInputState();
			if (_inputState != inputState)
			{
				inputs.bindingMask = new InputBinding();
				_inputState        = inputState;
				ReadOnlyArray<InputActionMap> maps = Live.inputs.asset.actionMaps;
				for (var i = 0; i < maps.Count; i++)
				{
					if (inputState)
						maps[i].Enable();
					else
						maps[i].Disable();
				}
			}

			// UNITY CURSOR LOCK UPDATE
			// ----------------------------------------
			lockState = GetLockState();

			bool locked = lockState == LockState.Full;

			//look.Enable(locked);

			if (!GameOptions.current.mouselock_disable)
			{
				Cursor.visible = !locked;
#if UNITY_EDITOR_LINUX
				// There is a bug in linux right now where locked does not report any mouse delta
				// https://issuetracker.unity3d.com/issues/linux-inputsystems-mouse-delta-values-do-not-change-when-the-cursor-lockstate-is-set-to-locked
				Cursor.lockState = CursorLockMode.None;
#else
				Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
#endif
			}
			else
			{
				Cursor.visible   = true;
				Cursor.lockState = CursorLockMode.None;
			}

			_prevFocused = Application.isFocused;
		}

		private void LateUpdate()
		{
			Keyboard.current.SetIMEEnabled(false);

			foreach (Button btn in Button.All) btn.Update();
			foreach (Joystick joy in Joystick.All) joy.Update();
			foreach (AnalogAxis axis in AnalogAxis.All) axis.Update();
		}

		private enum LockState
		{
			/// <summary>
			/// Mouse cannot lock into the window, no inputs can be received.
			/// </summary>
			None,

			/// <summary>
			/// Mouse is unlocked from the window, but we can still send inputs to the game with gamepad and keyboard.
			/// </summary>
			Partial,

			/// <summary>
			/// Mouse is locked, and inputs can be sent to the game.
			/// </summary>
			Full
		}

		private static void FixEditorShortcutsOnWindows()
		{
#if UNITY_EDITOR_WIN
			// Workaround for being unable to use editor shortcuts without re-clicking unity windows after escaping out
			// (multi-window unity setup, with panes docked out)

			const string ClassName = "UnityContainerWndClass";

			IntPtr        ptr = IntPtr.Zero;
			StringBuilder sbClass = new StringBuilder(ClassName.Length + 1);
			StringBuilder sbText = new StringBuilder(1024);

			WindowsUtil.EnumWindows((wnd, param) =>
			{
				WindowsUtil.GetClassName(wnd, sbClass, sbClass.Capacity);
				if (sbClass.ToString() == ClassName)
				{
					// We found a Unity window!
					ptr = wnd;

					WindowsUtil.GetWindowText(ptr, sbText, sbText.Capacity);
					return sbText.ToString().Contains(" Unity "); // Continue searching if this is the main Unity window
				}

				// Continue searching
				return true;
			}, IntPtr.Zero);

			if (ptr != IntPtr.Zero)
			{
				WindowsUtil.SetForegroundWindow(ptr);
			}

			// assembly     assembly = typeof(editorwindow).assembly;
			// type         type     = assembly.gettype("unityeditor.inspectorwindow");
			// editorwindow gameview = editorwindow.getwindow(type);
			//
			// gameview.focus();
#endif
		}

		public static Vector2 GetCapturedMouseDelta()
		{
			if (Exists && MouseEnabled)
				return Mouse.current.delta.ReadValue();

			return Vector2.zero;
		}

		public static float GetCapturedScrollDelta()
		{
			if (Exists && MouseEnabled)
				return Mouse.current.scroll.ReadValue().y;
			return 0;
		}

		public static Vector2 GetMousePosition(Vector2 @default = default)
		{
			return GetMousePosition(out Vector2 p) ? p : @default;
		}

		public static bool GetMousePosition(out Vector2 mousepos)
		{
			if (ActiveDevice == InputDevices.KeyboardAndMouse && Mouse.current != null)
			{
				mousepos = Mouse.current.position.ReadValue();
				return true;
			}
			else
			{
				mousepos = Vector2.zero;
				return false;
			}
		}

		public static void SetMouseUnlock(string id, bool b = true)
		{
			mouseUnlocks.Set(id, b);
		}

	#region Saving And Loading

		private const  string       INPUT_FILE_NAME = "/inputs.ini";
		static private List<string> _bufferKeyboard = new List<string>();
		static private List<string> _bufferGamepad  = new List<string>();
		static private List<string> _bufferOther    = new List<string>();

		public void RestoreAllDefaultBindingsAndSave()
		{
			inputs.RemoveAllBindingOverrides();
			SaveBindings();
		}

		[Button]
		public void SaveBindings()
		{
			INIParser parser = new INIParser();

			var path = Application.persistentDataPath + INPUT_FILE_NAME;

			if (File.Exists(path))
			{
				File.WriteAllText(path, String.Empty);
			}

			parser.Open(path);

			foreach (IHasInputAction has in AllInputs)
			{
				InputAction action = has.InputAction;

				_bufferKeyboard.Clear();
				_bufferGamepad.Clear();
				_bufferOther.Clear();

				for (var i = 0; i < action.bindings.Count; i++)
				{
					InputBinding binding = action.bindings[i];
					if (binding.isComposite || !binding.hasOverrides) continue;

					if (binding.groups == null)
					{
						_bufferOther.Add(i + ":" + binding.overridePath);
						continue;
					}

					if (binding.groups.Contains(GROUP_KEYBOARD))
					{
						_bufferKeyboard.Add(i + ":" + binding.overridePath);
					}
					else if (binding.groups.Contains(GROUP_GAMEPAD))
					{
						_bufferGamepad.Add(i + ":" + binding.overridePath);
					}
				}

				if (_bufferKeyboard.Count > 0)
					parser.WriteValue(GROUP_KEYBOARD, action.name, _bufferKeyboard.JoinString());

				if (_bufferGamepad.Count > 0)
					parser.WriteValue(GROUP_GAMEPAD, action.name, _bufferGamepad.JoinString());

				if (_bufferOther.Count > 0)
					parser.WriteValue("other", action.name, _bufferOther.JoinString());
			}

			parser.Close();
		}


		[Button]
		public void LoadBindings()
		{
			INIParser parser = new INIParser();
			parser.Open(Application.persistentDataPath + INPUT_FILE_NAME);

			foreach (IHasInputAction has in AllInputs)
			{
				InputAction action = has.InputAction;

				void Parse(string group)
				{
					string overrides = parser.ReadValue(group, action.name, "");
					if (!overrides.IsNullOrWhitespace())
					{
						var allOverrides = overrides.Split(',');
						for (int j = 0; j < allOverrides.Length; j++)
						{
							string str = allOverrides[j];

							int binding_index = -1;

							if (str.Contains(':'))
							{
								int ind = str.IndexOf(':');
								binding_index = Convert.ToInt32(str.Substring(0, ind));
								str           = str.Substring(ind + 1);
							}

							if (binding_index >= 0)
							{
								if (!str.IsNullOrWhitespace())
								{
									action.ApplyBindingOverride(binding_index, str);
								}
								else
								{
									action.RemoveBindingOverride(binding_index);
								}
							}
							else
							{
								//TODO Maybe
								//action.AddBinding(str, null, null, group);
							}
						}
					}
				}

				Parse(GROUP_GAMEPAD);
				Parse(GROUP_KEYBOARD);
			}

			parser.Close();
		}

	#endregion

		private InputActionRebindingExtensions.RebindingOperation _rebindingOperation;

		public void OnLayout(ref DebugSystem.State state)
		{
			if (state.Begin("Inputs"))
			{
				if (_rebindingOperation != null && _rebindingOperation.started)
				{
					g.Text("REBINDING: " + _rebindingOperation.action.name);
				}

				if (g.Button("Save"))
				{
					SaveBindings();
				}
				else if (g.Button("Load"))
				{
					LoadBindings();
				}
				else if (g.Button("Reset to Default"))
				{
					RestoreAllDefaultBindingsAndSave();
				}

				g.Separator();

				for (var index = 0; index < AllInputs.Count; index++)
				{
					IHasInputAction inputs = AllInputs[index];
					g.PushID(index);
					InputAction action = inputs.InputAction;
					g.Text(action.name);
					g.SameLine();

					if (g.Button("Rebind All"))
					{
						_rebindingOperation = action.PerformInteractiveRebinding()
							.OnComplete(x => x.Dispose());

						_rebindingOperation.Start();
					}

					g.SameLine();
					if (g.Button("Cancel Overrides"))
					{
						action.RemoveAllBindingOverrides();
					}

					g.Indent(24);
					for (var i = 0; i < action.bindings.Count; i++)
					{
						InputBinding binding = action.bindings[i];
						g.PushID(i);
						if (g.Button("Rebind"))
						{
							_rebindingOperation = action.PerformInteractiveRebinding()
								.WithTargetBinding(i)
								.OnComplete(x => x.Dispose())
								.Start();
						}

						g.SameLine();

						if (g.Button("Clear"))
						{
							action.RemoveBindingOverride(i);
						}

						g.SameLine();

						g.Text($"name: {binding.name}, groups: {binding.groups}, action: {binding.action}, path: {binding.path}, hasOverrides: {binding.hasOverrides}, overridePath: {binding.overridePath}");
						//g.SameLine();
						g.PopID();
					}

					g.Unindent(24);
					g.Separator();
					g.PopID();
				}

				g.End();
			}
		}

		[DebugRegisterGlobals]
		public static void RegisterMenu() { }

		public abstract class Button
		{
			protected bool  isPressed;
			protected bool  isDown;
			protected bool  isReleased;
			protected float muteTimer;
			protected float heldSeconds;

			public static readonly List<Button> All = new List<Button>();

			internal Button()
			{
				All.Add(this);
			}

			public virtual bool IsPressed  => muteTimer <= 0 && isPressed;
			public virtual bool IsDown     => muteTimer <= 0 && isDown;
			public virtual bool IsReleased => muteTimer <= 0 && isReleased;

			public bool IsUp => !IsDown;

			/// <summary>
			/// Returns true if the button is pressed this frame, or if it has been held for the specified duration.
			/// </summary>
			/// <param name="holdDuration">How long to have held the button, defaults to GameInputs.DEFAULT_HOLD_DELAY</param>
			/// <returns></returns>
			public bool IsPressedOrHeld(float? holdDuration = null)
			{
				return IsPressed || IsHeld(holdDuration);
			}

			/// <summary>
			/// Returns true if the button has been held for the specified duration.
			/// </summary>
			/// <param name="duration">How long to have held the button, defaults to GameInputs.DEFAULT_HOLD_DELAY</param>
			/// <param name="resets"></param>
			/// <returns></returns>
			public bool IsHeld(float? duration = null, bool resets =false)
			{
				bool ret = heldSeconds > (duration ?? DEFAULT_HOLD_DELAY);
				if (ret && resets)
				{
					heldSeconds = 0;
				}

				return ret;
			}

			public virtual void Reset()
			{
				isDown     = false;
				isReleased = false;
				isPressed  = false;
			}

			public virtual void Update()
			{
				muteTimer -= Time.deltaTime;

				if (IsDown)
					heldSeconds += Time.deltaTime;
				else
					heldSeconds = 0;
			}

			public void Mute(float f)
			{
				muteTimer = f;
			}
		}

		public interface IHasInputAction
		{
			InputAction InputAction { get; }
		}

		public class GenericButton : Button
		{
			public void OnPressed()
			{
				if (IsDown) return;

				isDown    = true;
				isPressed = true;
			}


			public void OnReleased()
			{
				if (!IsDown) return;

				isReleased = true;
				isDown     = false;
			}

			public override void Update()
			{
				base.Update();

				isReleased = false;
				isPressed  = false;
			}
		}

		/// <summary>
		/// Represents a button in the game.
		/// Can be pressed, held, and released.
		/// </summary>
		public class ActionButton : Button, IHasInputAction
		{
			public InputAction InputAction { get; }

			internal ActionButton(InputAction action)
			{
				AllInputs.Add(this);
				InputAction = action;

				action.started += context =>
				{
					if (muteTimer > Mathf.Epsilon)
						return;

					isPressed  = true;
					isReleased = false;
				};

				action.performed += context =>
				{
					if (muteTimer > Mathf.Epsilon)
						return;

					isPressed  = true;
					isReleased = false;
				};

				action.canceled += context =>
				{
					isPressed  = false;
					isReleased = true;
				};
			}

			public override bool IsDown => muteTimer <= 0 && InputAction.ReadValue<float>() > 0.25f;

			public string GetBindingDisplayString(InputBinding.DisplayStringOptions options = InputBinding.DisplayStringOptions.IgnoreBindingOverrides)
			{
				switch (LastRealDevice)
				{
					case InputDevices.Gamepad:
						return InputAction.GetBindingDisplayString(options, @group: "Gamepad");
					case InputDevices.KeyboardAndMouse:
						return InputAction.GetBindingDisplayString(options, @group: "KeyboardMouse");
					default:
						return InputAction.GetBindingDisplayString(options);
				}
			}

			public override void Update()
			{
				base.Update();

				isPressed  = false;
				isReleased = false;
			}

			public bool AbsorbPress(float muteTime = 0)
			{
				bool pressed = IsPressed;

				if (pressed)
					muteTimer = muteTime;

				isPressed = false;
				return pressed;
			}
		}

		/// <summary>
		/// Represents a joystick i nthe game.
		/// Has 4 button directions and 1 analog vector.
		/// </summary>
		public class Joystick : IHasInputAction
		{
			public static readonly List<Joystick> All = new List<Joystick>();

			public readonly GenericButton left  = new GenericButton();
			public readonly GenericButton right = new GenericButton();
			public readonly GenericButton up    = new GenericButton();
			public readonly GenericButton down  = new GenericButton();

			public bool withMouseDelta;

			public InputAction InputAction { get; }

			public Joystick(InputAction action)
			{
				All.Add(this);
				AllInputs.Add(this);

				InputAction = action;
			}

			public Vector2 Value
			{
				get
				{
					Vector2 ret = InputAction.ReadValue<Vector2>();
					if (withMouseDelta && Mouse.current != null)
						ret += Mouse.current.delta.ReadValue();

					return ret;
				}
			}

			public float Horizontal => Value.x;
			public float Vertical   => Value.y;

			public void Enable(bool b)
			{
				if (b) InputAction.Enable();
				else InputAction.Disable();
			}

			public void Update()
			{
				Vector2 v = Value;

				if (v.x > Mathf.Epsilon) right.OnPressed();
				else right.OnReleased();

				if (v.x < -Mathf.Epsilon) left.OnPressed();
				else left.OnReleased();

				if (v.y > Mathf.Epsilon) up.OnPressed();
				else up.OnReleased();

				if (v.y < -Mathf.Epsilon) down.OnPressed();
				else down.OnReleased();
			}

			public bool AnyPressed => left.IsPressed || right.IsPressed ||
			                          up.IsPressed || down.IsPressed;

			public string GetBindingDisplayString(InputBinding.DisplayStringOptions options = InputBinding.DisplayStringOptions.IgnoreBindingOverrides)
			{
				switch (LastRealDevice)
				{
					case InputDevices.Gamepad:
						return InputAction.GetBindingDisplayString(options, @group: "Gamepad");
					case InputDevices.KeyboardAndMouse: {

						InputBinding mask = new InputBinding{groups = "KeyboardMouse"};

						var result          = string.Empty;
						var bindings        = InputAction.bindings;
						var first_composite = true;

						for (var i = 0; i < bindings.Count; ++i)
						{
							if (bindings[i].isComposite) {
								if (!first_composite) {
									result = $"{result}\n";
								}

								var first = true;
								i++;
								while (i < bindings.Count && bindings[i].isPartOfComposite) {

									if (!mask.Matches(bindings[i]))
										continue;

									string text = InputAction.GetBindingDisplayString(i, options);
									if (result != "") {
										if(first)
											result = $"{result}{text}";
										else
											result = $"{result}, {text}";
									}else
										result = text;

									first = false;
									i++;
								}

								first_composite = false;
								i--;
								continue;
							}
						}

						return result;
					}

					default:
						return InputAction.GetBindingDisplayString(options);
				}

			}
		}

		public class AnalogAxis : IHasInputAction
		{
			public static readonly List<AnalogAxis> All = new List<AnalogAxis>();

			public readonly GenericButton positive = new GenericButton();
			public readonly GenericButton negative = new GenericButton();

			public InputAction InputAction { get; }

			public float Value => InputAction.ReadValue<float>();

			public AnalogAxis(InputAction action)
			{
				AllInputs.Add(this);
				All.Add(this);

				InputAction = action;
			}

			public void Reset()
			{
				positive.Reset();
				negative.Reset();
			}

			public void Enable(bool b)
			{
				if (b) InputAction.Enable();
				else InputAction.Disable();
			}

			public void Update()
			{
				float v = Value;

				if (v > Mathf.Epsilon) positive.OnPressed();
				else positive.OnReleased();

				if (v < -Mathf.Epsilon) negative.OnPressed();
				else negative.OnReleased();
			}

			public bool AnyPressed => positive.IsPressed || negative.IsPressed;
		}
	}
}