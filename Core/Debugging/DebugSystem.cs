using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Anjin.Nanokin;
using Anjin.UI;
using Anjin.Util;
using Core.Debug;
using ImGuiNET;
using JetBrains.Annotations;
using Overworld.Cutscenes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Debug = UnityEngine.Debug;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class DebugSystem : StaticBoy<DebugSystem>
{
	private const int FPS_BUFFER_SIZE = 60;

	/// <summary>
	/// A layout handler event which is always.
	/// </summary>
	public static ImGuiLayoutHandler onLayout;

	/// <summary>
	/// A layout handler event which is invoked only outside of debug mode.
	/// </summary>
	public static ImGuiLayoutHandler onLayoutNormal;

	/// <summary>
	/// A layout handler event which is invoked only in debug mode.
	/// </summary>
	public static ImGuiLayoutHandler onLayoutDebug;

	/// <summary>
	/// A list of typed handlers which can hook the layout process.
	/// Handlers that are Unity components will be automatically removed when the component is destroyed.
	/// </summary>
	public static List<IDebugDrawer> drawers;

	/// <summary>
	/// Current FPS. (high frequency)
	/// </summary>
	public static float fps;

	/// <summary>
	/// Current FPS. (average during last second)
	/// </summary>
	public static int fpsAverage;

	/// <summary>
	/// Current FPS. (highest during last second)
	/// </summary>
	public static int fpsHighest;

	/// <summary>
	/// Current FPS. (lowest during last second)
	/// </summary>
	public static int fpsLowest;

	private State            _state;
	private int[]            _fpsBuffer;
	private int              _fpsBufferIndex;
	private PointerEventData _cachedPointerData;

	private static List<RaycastResult> _raycastResults;

	public static bool Opened => Live._state.DebugMode;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void Init()
	{
		drawers         = new List<IDebugDrawer>();
		_raycastResults = new List<RaycastResult>();
	}

	private void OnApplicationQuit()
	{
		onLayout       = null;
		onLayoutNormal = null;
		onLayoutDebug  = null;
		drawers.Clear();

		fps        = 0;
		fpsAverage = 0;
		fpsHighest = 0;
		fpsLowest  = 0;

		_fpsBufferIndex = 0;
		_fpsBuffer      = null;

		_raycastResults = null;
	}

	protected override void OnAwake()
	{
		_fpsBuffer      = new int[FPS_BUFFER_SIZE];
		_state.Menus    = new Dictionary<string, bool>();
		_state.AllMenus = new List<string>();

		// Register all IDebugDrawer types
		// ----------------------------------------

#if UNITY_EDITOR
		// This is much faster than the alternative
		TypeCache.MethodCollection staticMethods = TypeCache.GetMethodsWithAttribute(typeof(DebugRegisterGlobalsAttribute));
		foreach (MethodInfo mi in staticMethods)
		{
			if (mi.IsStatic)
				mi.Invoke(null, null);
		}
#else
		Type[]            types       = Assembly.GetAssembly(typeof(DebugSystem)).GetTypes();
		IEnumerable<Type> drawerTypes = types.Where(x => typeof(IDebugDrawer).IsAssignableFrom(x));

		foreach (Type type in drawerTypes)
		{
			MethodInfo[] methods = type
				.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
				.Where(m => m.GetCustomAttributes(typeof(DebugRegisterGlobalsAttribute), false).Length > 0).ToArray();

			foreach (MethodInfo info in methods)
			{
				info.Invoke(null, null);
			}
		}
#endif


		RegisterMenu("Debug State");
		RegisterMenu("ImGUI Demo");
	}

	private void Start()
	{
		ImGuiUn.Layout += Layout;
		ImGuiThemes.Cherry();
	}

	private void Update()
	{
		// Update Debug UI mouse input locks
		// ----------------------------------------
		GameInputs.forceUnlocks.Set("debug_mode", Opened);

		if (Mouse.current.rightButton.wasPressedThisFrame)
		{
			GameInputs.forceLocks.Add("debug_mouse_click");
		}
		else if (!Mouse.current.rightButton.isPressed)
		{
			GameInputs.forceLocks.Remove("debug_mouse_click");
		}

		// Update FPS calculations
		// ----------------------------------------
		fps = 1f / Time.unscaledDeltaTime;

		_fpsBuffer[_fpsBufferIndex++] = (int)fps;
		if (_fpsBufferIndex >= FPS_BUFFER_SIZE)
			_fpsBufferIndex = 0;

		int sum     = 0;
		int highest = 0;
		int lowest  = int.MaxValue;

		for (int i = 0; i < FPS_BUFFER_SIZE; i++)
		{
			int fps = _fpsBuffer[i];

			sum += fps;
			if (fps > highest)
				highest = fps;

			if (fps < lowest)
				lowest = fps;
		}

		fpsAverage = sum / FPS_BUFFER_SIZE;
		fpsHighest = highest;
		fpsLowest  = lowest;
	}

	private void OnDisable()
	{
		ImGuiUn.Layout -= Layout;
	}

	private void Layout()
	{
		_state.DebugMode   = GameController.DebugMode;
		_state.DisplaySize = new Vector2(Screen.width, Screen.height);

		// Auto-remove MonoBehaviour drawers that have been cleaned up or destroyed
		for (int i = 0; i < drawers.Count; i++)
		{
			if (drawers[i] == null || drawers[i] is MonoBehaviour mono && mono == null)
				drawers.RemoveAt(i--);
		}

		onLayout?.Invoke(ref _state);
		if (_state.DebugMode)
		{
			DrawMenuBar(ref _state);

			ImDrawListPtr draw = ImGui.GetBackgroundDrawList();

			void _label(string text, Color color, Vector2 pos, float scale = 1)
			{
				float size = ImGui.GetFontSize() * scale;
				draw.AddText(ImGui.GetFont(), size, pos + new Vector2(1, 0), Color.black.ToUint(), text);
				draw.AddText(ImGui.GetFont(), size, pos + new Vector2(-1, 0), Color.black.ToUint(), text);
				draw.AddText(ImGui.GetFont(), size, pos + new Vector2(0, 1), Color.black.ToUint(), text);
				draw.AddText(ImGui.GetFont(), size, pos + new Vector2(0, -1), Color.black.ToUint(), text);

				draw.AddText(ImGui.GetFont(), size, pos + new Vector2(1, 1), Color.black.ToUint(), text);
				draw.AddText(ImGui.GetFont(), size, pos + new Vector2(-1, 1), Color.black.ToUint(), text);
				draw.AddText(ImGui.GetFont(), size, pos + new Vector2(1, -1), Color.black.ToUint(), text);
				draw.AddText(ImGui.GetFont(), size, pos + new Vector2(-1, -1), Color.black.ToUint(), text);
				draw.AddText(ImGui.GetFont(), size, pos, color.ToUint(), text);
			}

			const int XMARGIN = 8;
			const int YMARGIN = 22;
			const int LINE    = 12;

			// FPS INFORMATION
			// ----------------------------------------
			_label($"FPS: {fps:F0} (avg {fpsAverage} / high {fpsHighest} / low {fpsLowest})", ColorsXNA.OrangeRed, new Vector2(XMARGIN, YMARGIN));


			// UGUI INFORMATION
			// ----------------------------------------
			EventSystem sys = EventSystem.current;

			_cachedPointerData          = _cachedPointerData ?? new PointerEventData(sys);
			_cachedPointerData.position = Input.mousePosition;
			sys.RaycastAll(_cachedPointerData, _raycastResults);

			string raycastStr = _raycastResults.Count > 0
				? _raycastResults.JoinString(selector: x => x.gameObject.name)
				: string.Empty;

			_label($"UGUI SELECTED: {EventSystem.current.currentSelectedGameObject}", ColorsXNA.OrangeRed, new Vector2(XMARGIN, YMARGIN + LINE * 1));
			_label($"UGUI RAYCAST: {raycastStr}", ColorsXNA.OrangeRed, new Vector2(XMARGIN, YMARGIN + LINE * 2));

			onLayoutDebug?.Invoke(ref _state);
		}
		else
		{
			onLayoutNormal?.Invoke(ref _state);
		}

		for (int i = 0; i < drawers.Count; i++)
		{
			drawers[i].OnLayout(ref _state);
		}

		if (_state.IsMenuOpen("ImGUI Demo"))
			ImGui.ShowDemoWindow();

		if (_state.IsMenuOpen("Debug State"))
		{
			if (ImGui.Begin("Demo"))
			{
				AImgui.DrawDemo();
			}

			/*if (ImGui.Begin("Debug System State"))
			{
				//AnjinGui.EditStruct(ref _state);
			}*/

			ImGui.End();
		}
	}

	public void DrawMenuBar(ref State state)
	{
		// MAIN MENU
		// ----------------------------------------
		if (ImGui.BeginMainMenuBar())
		{
			if (ImGui.BeginMenu("Main"))
			{
				if (ImGui.MenuItem("Despawn", "F1")) GameController.Live.DespawnToSpawnMenu();
				if (ImGui.MenuItem("To to level select")) GameController.Live.ExitGameplayToMenu(GameAssets.Live.DebugLevelSelectMenuScene);

				/*if (ImGui.MenuItem("Restart Game")) {
					SceneManager.LoadScene(0);
				}*/

				ImGui.EndMenu();
			}

			if (ImGui.BeginMenu("Menus"))
			{
				for (int i = 0; i < state.AllMenus.Count; i++)
				{
					string key = state.AllMenus[i];

					bool sel = state.Menus[key];
					ImGui.MenuItem(key, "", ref sel, true);
					state.Menus[key] = sel;
				}

				ImGui.EndMenu();
			}

			if (ImGui.BeginMenu("Cutscenes"))
			{
				Cutscene cutscene = GameController.Live.ControllingCutscene;

				if (ImGui.MenuItem("Skip current Cutscene (f11)",
					    cutscene != null &&
					    cutscene.coplayer != null &&
					    cutscene.coplayer.IsPlaying &&
					    !cutscene.coplayer.Skipping)
				   )
				{
					cutscene.coplayer.StartSkipping();
				}

				ImGui.MenuItem("Character Bust Debug", "", ref CharacterBustAnimator.BustsDebugEnabled);

				ImGui.EndMenu();
			}

			if (ImGui.BeginMenu("Tools"))
			{
#if UNITY_EDITOR
				if (DebugRecorder.Exists)
				{
					if (DebugRecorder.IsRecording)
					{
						if (ImGui.MenuItem("Stop Recording"))
						{
							DebugRecorder.Stop();
						}
					}
					else
					{
						if (ImGui.MenuItem("Start Recording"))
						{
							DebugRecorder.Begin();
						}
					}
				}
#endif

				ImGui.EndMenu();
			}


			if (ImGui.BeginMenu("Performance"))
			{
				if (ImGui.MenuItem("GC Collect"))
				{
					// This was a cached field previously so we can re-use the same stowatch each time.
					// It's a micro-optimization but in cases like this it's usually better to instantiate as you need
					// because otherwise it's taking memory all the time even though it's barely ever used. (and the object
					// never leaves gen 0 of the gc anyway)
					Stopwatch sw = Stopwatch.StartNew();

					sw.Start();
					GC.Collect();
					sw.Stop();

					Debug.Log($"GC Collect Time: {sw.ElapsedMilliseconds}, {sw.ElapsedTicks}");

					sw.Reset();
				}

				ImGui.EndMenu();
			}

			if (ImGui.BeginMenu("Misc"))
			{
				ImGui.MenuItem("Show ImGUI Demo Window", "", ref state.DemoMenuOpen);
				ImGui.EndMenu();
			}

			//ImGui.Text("GC Currently Allocated: "+GC.GetTotalMemory(false));

			ImGui.EndMainMenuBar();
		}


		// CONTEXT MENU TO OPEN MENUS
		// ----------------------------------------
		if (ImGui.IsMouseDown(ImGuiMouseButton.Middle))
		{
			ImGui.OpenPopup("menus_popup");
		}

		if (ImGui.BeginPopup("menus_popup"))
		{
			foreach (string menu in state.AllMenus)
			{
				if (ImGui.Selectable(menu, state.Menus[menu]))
				{
					state.Menus[menu] = !state.Menus[menu];
				}
			}
		}
	}

	public static void Register(IDebugDrawer drawer)
	{
		drawers.Add(drawer);
	}

	public static void Unregister(IDebugDrawer drawer)
	{
		drawers.Remove(drawer);
	}

	public static void RegisterMenu(string name)
	{
		Live._state.RegisterMenu(name);
	}

	public static void ToggleMenu(string name)
	{
		Live._state.ToggleMenu(name);
	}

	public static void OpenMenu(string name, bool state = true)
	{
		Live._state.OpenMenu(name, state);
	}

	public delegate void ImGuiLayoutHandler(ref State state);

	public struct State
	{
		public bool                     DebugMode;
		public Vector2                  DisplaySize;
		public bool                     DemoMenuOpen;
		public Dictionary<string, bool> Menus;
		public List<string>             AllMenus;

		public void OpenMenu(string name, bool state)
		{
			if (!Menus.ContainsKey(name))
				RegisterMenu(name);

			Menus[name] = state;
			DebugMode   = true;
		}

		// Put stuff here so the system can talk to drawers
		public void ToggleMenu(string name)
		{
			if (!Menus.ContainsKey(name))
				RegisterMenu(name);

			Menus[name] = !Menus[name];
			DebugMode   = true;
		}

		public bool IsMenuOpen(string name)
		{
			if (Menus.TryGetValue(name, out bool val))
			{
				return val;
			}
			else
			{
				RegisterMenu(name);
				return false;
			}
		}

		public void RegisterMenu(string name)
		{
			Menus[name] = false;
			AllMenus.Add(name);
			AllMenus.Sort();
		}

		public bool Begin(string name)
		{
			if (!Menus.TryGetValue(name, out bool state))
			{
				state = Menus[name] = false;
				AllMenus.Add(name);
				AllMenus.Sort();
			}

			if (state)
			{
				bool ret = ImGui.Begin(name, ref state);
				if (!state)
					Menus[name] = false;

				return ret;
			}

			return false;
		}
	}
}

public interface IDebugDrawer
{
	void OnLayout(ref DebugSystem.State state);
}

[AttributeUsage(AttributeTargets.Method)]
[MeansImplicitUse]
public class DebugRegisterGlobalsAttribute : Attribute { }