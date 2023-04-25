using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Anjin.Audio;
using Core.Debug;
using Cysharp.Threading.Tasks;
using ImGuiNET;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;
using static Core.Debug.DebugConsole;

[SuppressMessage("ReSharper", "InconsistentNaming")]
[DefaultExecutionOrder(-1)]
public class GameOptions : StaticBoyUnity<GameOptions>
{
	public const string DBG_NAME = "Options";

	public static Container current = new Container();

	// Container is serialized because it's not just a container for
	// the option values, but also defines display names for them
	// which are localized with GameText
	[SerializeField]
	private Container SerializedModel = new Container();

	[ShowInInspector]
	[Inline]
	[BoxGroupExt("File", 0.3f, 0.7f, 1.0f, Foldable = false)]
	public static INIFile _file;

	private static bool _isSaving;

	[Serializable]
	public class Container
	{
		public INIFloat dummy = new INIFloat("Game", "dummy", 0);

		// Display
		// ----------------------------------------

		// References an index in a static list of resolutions in GameController
		// Temporary, we should probably have a different way of storing this
		public INIFloat screen_resolution = new INIFloat("Screen Resolution", "screen_resolution", 0);

		public INIFloat screen_mode = new INIFloat("Screen Mode", "screen_mode", 0); //0: Windowed, 1: Borderless Fullscreen, 2: Fullscreen

		// Visual
		// ----------------------------------------
		public INIBool sprite_tilting        = new INIBool("Visual", "sprite_tilting", true);
		public INIBool sprite_tilting_ground = new INIBool("Visual", "sprite_tilting_ground", false);

		// Audio
		// ----------------------------------------
		public INIFloat audio_level_master  = new INIFloat("Audio", "audio_level_master", AudioManager.AUDIO_LEVEL_DEFAULT);
		public INIFloat audio_level_sfx     = new INIFloat("Audio", "audio_level_sfx", AudioManager.AUDIO_LEVEL_DEFAULT);
		public INIFloat audio_level_music   = new INIFloat("Audio", "audio_level_music", AudioManager.AUDIO_LEVEL_DEFAULT);
		public INIFloat audio_level_ambient = new INIFloat("Audio", "audio_level_ambient", AudioManager.AUDIO_LEVEL_DEFAULT);
		public INIFloat audio_level_voice   = new INIFloat("Audio", "audio_level_voice", AudioManager.AUDIO_LEVEL_DEFAULT);
		public INIFloat audio_speaker_mode  = new INIFloat("Audio", "audio_speaker_mode", (int)AudioSpeakerMode.Stereo);

		// Inputs
		// ----------------------------------------
		public INIBool run_by_default = new INIBool("Input", "run_by_default", false);

		public INIFloat CameraSensitivity  = new INIFloat("Input", "camera_sensitivity", 2.75f);
		public INIBool  CameraPadInvertX   = new INIBool("Input", "invert_xcam_pad", false);
		public INIBool  CameraPadInvertY   = new INIBool("Input", "invert_ycam_pad", false);
		public INIBool  CameraMouseInvertX = new INIBool("Input", "invert_xcam_mouse", false);
		public INIBool  CameraMouseInvertY = new INIBool("Input", "invert_ycam_mouse", false);
		public INIBool  CameraAuto         = new INIBool("Input", "auto_camera", false);

		public INIBool InvertFPCamXAxis_Pad   = new INIBool("Input", "InvertFirstPersonCamXAxis_Pad", false);
		public INIBool InvertFPCamYAxis_Pad   = new INIBool("Input", "InvertFirstPersonCamYAxis_Pad", false);
		public INIBool InvertFPCamXAxis_Mouse = new INIBool("Input", "InvertFirstPersonCamXAxis_Mouse", false);
		public INIBool InvertFPCamYAxis_Mouse = new INIBool("Input", "InvertFirstPersonCamYAxis_Mouse", false);

		// Gameplay
		// ----------------------------------------
		public INIString netplay_username         = new INIString("Internal", "username", "bob");
		public INIBool   combat_intro             = new INIBool("Game", "combat_arena_intro", true);
		public INIBool   combat_memory_cursors    = new INIBool("Game", "combat_memory_cursor", true);
		public INIBool   combat_merge_groupturns  = new INIBool("Game", "combat_merge_groupturns", true);
		public INIBool   splicer_menu_transitions = new INIBool("Game", "splicer_menu_transitions", true);


		// Developer Cheats
		// ----------------------------------------
		public INIBool ow_party             = new INIBool("Toggles", "ow_party", true);
		public INIBool ow_guests            = new INIBool("Toggles", "ow_guests", true);
		public INIBool ow_encounters        = new INIBool("Toggles", "ow_encounters", true);
		public INIBool ow_encounters_launch = new INIBool("Toggles", "ow_encounters_launch", true);
		public INIBool ow_encounters_helix  = new INIBool("Toggles", "ow_encounters_helix", true);

		public INIBool combat_base_mechanics = new INIBool("Toggles", "combat_base_mechanics", true);
		public INIBool combat_passives       = new INIBool("Toggles", "combat_passives", true);
		public INIBool combat_use_cost       = new INIBool("Toggles", "combat_use_cost", true);
		public INIBool combat_hurt           = new INIBool("Toggles", "combat_hurt", true);
		public INIBool combat_deaths         = new INIBool("Toggles", "combat_deaths", true);
		public INIBool combat_camera_motions = new INIBool("Toggles", "combat_camera_motions", false);
		public INIBool use_test_dummy_bust   = new INIBool("Toggles", "use_test_dummy_bust", true);

		public INIBool  combat_skill_unlocks  = new INIBool("Cheat", "combat_skill_unlocks", false);
		public INIBool  combat_use_loop       = new INIBool("Cheat", "combat_use_loop", false); // Note: hold TAB to disable while the skill is repeating
		public INIFloat combat_use_loop_delay = new INIFloat("Cheat", "combat_use_loop", 0.1f);
		public INIBool  combat_autowin        = new INIBool("Cheat", "combat_autowin", false);

		// Devtool Options
		// ----------------------------------------
		public INIBool  mouselock_on_startup   = new INIBool("DevTools", "mouselock_on_startup", false);
		public INIBool  mouselock_disable      = new INIBool("DevTools", "mouselock_disable", false);
		public INIBool  spawn_with_imgui       = new INIBool("DevTools", "spawn_with_imgui", true);
		public INIBool  spawn_with_priority    = new INIBool("DevTools", "spawn_with_priority", false);
		public INIBool  debug_ui_on_startup    = new INIBool("DevTools", "debug_ui_on_startup", true);
		public INIBool  autosave_devmode       = new INIBool("DevTools", "auto_save_devmode", false);
		public INIBool  autosave_on_quit       = new INIBool("DevTools", "autosave_on_quit", true);
		public INIFloat DebugCameraSensitivity = new INIFloat("DevTools", "debug_camera_sensitivity", 2.75f);
		public INIBool  autoreload_options     = new INIBool("DevTools", "debug_autoreload_options", false);

		// Performance Options
		// ----------------------------------------
		public INIBool load_on_demand            = new INIBool("Performances", "load_on_demand", false);
		public INIBool pool_on_demand            = new INIBool("Performances", "pool_on_demand", false);
		public INIBool init_on_demand            = new INIBool("Performances", "init_on_demand", false);
		public INIBool log_addressable_profiling = new INIBool("Performances", "log_addressable_profiling", false);
		public INIBool keep_lua_core             = new INIBool("Performances", "keep_lua_core", false);
		public INIBool combat_fast_warmup        = new INIBool("Performances", "combat_fast_warmup", false);
		public INIBool combat_fast_pace          = new INIBool("Performances", "combat_fast_pace", false);

		// Internal Data and Features
		// ----------------------------------------
		public INIBool   sprite_color_replacement = new INIBool("Internal", "sprite_color_replacement", true);
		public INIBool   splicer_hub_backdrop     = new INIBool("Internal", "splicer_hub_backdrop", true);
		public INIString default_savefile         = new INIString("Internal", "savefile", "debug");

		// Logging
		// ----------------------------------------
		public INIBool log_coplayer            = new INIBool("Log", "log_coplayer", false);
		public INIBool log_combat_turns        = new INIBool("Log", "log_combat_turns", false);
		public INIBool log_combat_instructions = new INIBool("Log", "log_combat_instructions", false);
		public INIBool log_combat_state        = new INIBool("Log", "log_combat_state", false);
		public INIBool log_combat_visuals      = new INIBool("Log", "log_combat_visuals", false);
		public INIBool log_combat_emit         = new INIBool("Log", "log_combat_emit", false);

		public Container Clone() => MemberwiseClone() as Container;
	}

	public static void InitializeThroughGameController()
	{
#if UNITY_EDITOR
		// oxy:
		// I have several option files that I use as presets and swap between
		// This reserializes them all so they have new or renamed options.
		foreach (string filename in Directory.EnumerateFiles(Application.persistentDataPath, "option_*.ini"))
		{
			string name = Path.GetFileNameWithoutExtension(filename);

			_file = new INIFile(name);
			Load();
			Save();
		}


		_file = null;
#endif

		Load();

#if UNITY_EDITOR
		if (current.autoreload_options)
		{
			// Allows automatically reloading option.ini upon modified
			var ini = new INIFile("option");
			FileSystemWatcher fswatch = new FileSystemWatcher(Application.persistentDataPath, ini.FileName)
			{
				NotifyFilter        = NotifyFilters.LastWrite,
				EnableRaisingEvents = true
			};

			fswatch.Changed += (sender, args) =>
			{
				if (!_isSaving) return;
				UniTask.Create(async () =>
				{
					await UniTask.SwitchToMainThread();
					Load();
				});
			};
		}
#endif
	}

	/// <summary>
	/// Load into 'current'.
	/// </summary>
	public static void Load()
	{
		if (Live != null)
			current = Live.SerializedModel;

		if (_file == null)
			_file = new INIFile("option");

		_file.InitFromContainerThenLoadFromFile(current);
		_file.Save(); // This immediately cleans up the file by removing bad keys or adding new ones

		// Default value
		if (current.CameraSensitivity.Value < Mathf.Epsilon)
			current.CameraSensitivity.Value = 2.75f;
	}

	/// <summary>
	/// Save 'current' to disk.
	/// </summary>
	public static void Save()
	{
		_isSaving = true;
		_file.Save();
		_isSaving = false;
	}

	private void Start()
	{
		DebugSystem.onLayoutDebug += OnLayout;

		AddCommand("options", (parser, io) => io.output.AddRange(_file.allValues));
		AddCommand("option", OptionCommand);
	}

	private void OnDestroy()
	{
		if (_file != null && current.autosave_on_quit)
			Save();

		DebugSystem.onLayoutDebug -= OnLayout;

		// Reset
		_file   = new INIFile("option");
		current = new Container();
	}

	public void OnLayout(ref DebugSystem.State state)
	{
		if (state.Begin(DBG_NAME))
		{
			AImgui.Text("INI File: ");
			ImGui.SameLine();
			if (current != null)
			{
				AImgui.Text(_file.FullPath, Color.green);
				if (ImGui.Button("Save")) _file.Save();
				ImGui.SameLine();
				if (ImGui.Button("Load")) _file.Load();

				string currentCategory = null;

				float longest_key = 0;
				for (int i = 0; i < _file.allValues.Count; i++)
				{
					var size                              = ImGui.CalcTextSize(_file.allValues[i].Key);
					if (size.x > longest_key) longest_key = size.x;
				}

				for (var i = 0; i < _file.allValues.Count; i++)
				{
					INIFile.INIValue value = _file.allValues[i];

					if (value.Category != currentCategory)
					{
						if (i != 0)
						{
							ImGui.Separator();
						}

						currentCategory = value.Category;
						ImGui.Text(value.Category);
						ImGui.NextColumn();
						ImGui.NextColumn();
						ImGui.NextColumn();
					}

					ImGui.PushID(i);

					ImGui.Columns(3);
					ImGui.SetColumnWidth(0, longest_key + 16);

					// The key instead of DisplayName since I think it's better for a debug menu to show exactly what's gonna be in the file
					AImgui.Text(value.Key, ColorsXNA.CornflowerBlue);
					ImGui.NextColumn();

					DrawValue(value);
					ImGui.NextColumn();

					if (value.HasTempValue)
						AImgui.Text("Temp", ColorsXNA.PaleGoldenrod);
					else
						AImgui.Text("-");

					ImGui.NextColumn();

					ImGui.PopID();
				}

				ImGui.Columns(0);
			}
			else
			{
				AImgui.Text("null", Color.red);
			}

			ImGui.End();
		}


		void DrawValue(INIFile.INIValue value)
		{
			if (value is INIBool boolVal)
			{
				bool val = boolVal.Value;
				ImGui.Checkbox("##hide", ref val);
				boolVal.Value = val;
			}
			else if (value is INIFloat floatVal)
			{
				float val = floatVal.Value;
				ImGui.InputFloat("##hide", ref val);
				floatVal.Value = val;
			}
			else if (value is INIString stringVal)
			{
				string val = stringVal.Value;
				ImGui.InputText("##hide", ref val, 1000);
				stringVal.Value = val;
			}
		}
	}

	private void OptionCommand(StringDigester parser, CommandIO io)
	{
		string                                    option   = parser.Word();
		(bool success, string left, string right) operands = StringDigester.FindOperands(option, '=');

		foreach (INIFile.INIValue value in _file.allValues)
		{
			string key = value.Key;
			if (value is INIBool inibool)
			{
				if (operands.success && operands.left == key)
				{
					bool parse;

					switch (operands.right)
					{
						case "yes":
							parse = true;
							break;

						case "no":
							parse = false;
							break;

						default:
							bool.TryParse(operands.right, out parse);
							break;
					}

					inibool.Value = parse;
					Log($"{key} = {value.ValueString}");
				}
				else if (option.EndsWith("!") && key == option.Substring(0, option.Length - 2))
				{
					// Toggle
					inibool.Value = !inibool.Value;
					Log($"{key} = {value.ValueString}");
				}
				else if (option.StartsWith("!") && key == option.Substring(1))
				{
					inibool.Value = false;
					Log($"{key} = {value.ValueString}");
				}
			}
			else if (value is INIFloat inifloat)
			{
				if (operands.success && operands.left == key)
				{
					inifloat.Value = float.Parse(operands.right);
					Log($"{key} = {value.ValueString}");
				}
			}
			else if (value is INIString inistr)
			{
				if (operands.success && operands.left == key)
				{
					inistr.Value = operands.right;
					Log($"{key} = {value.ValueString}");
				}
			}
		}
	}
}