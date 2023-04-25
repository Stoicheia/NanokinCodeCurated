using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Anjin.Nanokin;
using Anjin.Nanokin.Park;
using Anjin.Util;
using ImGuiNET;
using Overworld.Park_Game;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Vexe.Runtime.Extensions;
using g = ImGuiNET.ImGui;
#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;

#endif

namespace Core.Debug
{
	public class DebugConsole : StaticBoy<DebugConsole>
	{
		private static StringDigester _digester = new StringDigester();

		private bool                              _init;
		private bool                              _scrollToEnd;
		private bool                              _opened;
		private List<string>                      _entryHistory;
		private List<LogData>                     _buffer;
		private string                            _textInput;
		private float                             _opacity = 0.75f;
		private Dictionary<string, Command>       _commands;
		private Dictionary<object, string>        _commandsReverse;
		private Dictionary<object, List<string>>  _commandGroups;
		private Dictionary<string, bool>          _filters;
		private int                               _entryNavIndex;
		private Dictionary<Type, ObjectDrawer>    _objectDrawers;
		private Dictionary<Type, ObjectFormatter> _objectFormatters;
		private bool                              _reclaimFocus = true;
		private object                            _currentGroup;

		public delegate void ObjectDrawer(LogData obj);

		public delegate string ObjectFormatter(LogData obj);

		private float _lastScrollMax;
		private float _muteTimer;

		/// <summary>
		/// Whether the console is open or not.
		/// </summary>
		public bool Opened
		{
			get => _opened;
			set
			{
				if (_muteTimer > 0f) return;
				_muteTimer = 0.1f;

				GameInputs.forceUnlocks.Set("debug_console", value);
				GameInputs.inputDisables.Set("debug_console", value);

				_opened       = value;
				_scrollToEnd  = true;
				_reclaimFocus = true;
			}
		}

		/// <summary>
		/// Add a command.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="callback"></param>
		public static void AddCommand(string command, Action callback) //
		{
			AddCommand(command, (digest, io) => callback());
		}

		/// <summary>
		/// Add a command which can digest the user input and work with the IO lists for piping commands together.
		/// </summary>
		/// <param name="command"></param>
		/// <param name="callback"></param>
		public static void AddCommand(string command, Action<StringDigester, CommandIO> callback)
		{
			_digester.Reset(command);
			string name = _digester.Word();

			Live._commandsReverse[callback] = name;
			Live._commands.Add(name, new Command
			{
				expression = command,
				execute    = callback
			});

			if (Live._currentGroup != null)
			{
				Live._commandGroups[Live._currentGroup].Add(command);
			}
		}

		/// <summary>
		/// Remove a command by the callback.
		/// </summary>
		/// <param name="callback"></param>
		public static void RemoveCommand(Action callback)
		{
			string cmd = Live._commandsReverse[callback];
			Live._commands.Remove(cmd);
			Live._commandsReverse.Remove(callback);
		}

		/// <summary>
		/// Start a command group.
		/// Add following AddCommand will be added to this group until EndGroup().
		/// </summary>
		/// <returns></returns>
		public static object BeginGroup()
		{
			var group = new object();
			Live._commandGroups.Add(group, new List<string>());
			Live._currentGroup = group;
			return group;
		}

		/// <summary>
		/// End the current command group.
		/// </summary>
		public static void EndGroup()
		{
			Live._currentGroup = null;
		}

		/// <summary>
		/// Remove all commands associated to a command group.
		/// </summary>
		/// <param name="group"></param>
		public static void RemoveGroup(object group)
		{
			foreach (string name in Live._commandGroups[group])
			{
				Command cmd = Live._commands[name];
				Live._commandsReverse.Remove(cmd.execute);
				Live._commands.Remove(name);
			}

			Live._commandGroups.Remove(group);
		}

		protected override void OnAwake()
		{
			base.OnAwake();

			Application.logMessageReceived += (condition, trace, type) => {
				if (Live == null || Live._buffer == null) return;

				switch (type)
				{
					case LogType.Exception:
						Live._buffer.Add(new LogData
						{
							text   = condition.Split(new[] {"/n"}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(),
							data   = trace,
							color  = Color.red,
							drawer = DrawBoxedMessage
						});
						break;
					case LogType.Error:
						// Live._buffer.Add(new LogData
						// {
						// 	text   = condition.Split(new[] {"/n"}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(),
						// 	data   = trace,
						// 	color  = Color.red,
						// 	drawer = DrawBoxedMessage
						// });
						break;
					case LogType.Assert:

						break;
					case LogType.Warning:
						// Live._buffer.Add(new LogData
						// {
						// 	text   = condition.Split(new[] {"/n"}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault(),
						// 	data   = trace,
						// 	color  = Color.yellow,
						// 	drawer = DrawBoxedMessage
						// });
						break;
					case LogType.Log:
						Log("Log", condition);
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(type), type, null);
				}
			};

			_init = true;

			_textInput        = "";
			_buffer           = new List<LogData>();
			_entryHistory     = new List<string>();
			_commands         = new Dictionary<string, Command>();
			_commandsReverse  = new Dictionary<object, string>();
			_commandGroups    = new Dictionary<object, List<string>>();
			_filters          = new Dictionary<string, bool>();
			_objectDrawers    = new Dictionary<Type, ObjectDrawer>();
			_objectFormatters = new Dictionary<Type, ObjectFormatter>();
		}

		private void Start()
		{
			AddObjectDrawer<Scene>(DrawScene);
			AddObjectDrawer<GameObject>(DrawObject);
			AddObjectDrawer<Command>(DrawCommand);

			AddObjectToString<Scene>(scene => $"{scene.name} (roots = {scene.rootCount})");
			AddObjectToString<GameObject>(go => $"{go.name} (children = {go.transform.childCount}, components = {go.GetComponents<MonoBehaviour>().Length})");

			AddCommand("cls", Clear);
			AddCommand("clear", Clear);
			AddCommand("help", (digest, io) =>
			{
				io.output.AddRange(_commands.Values.OrderBy(c => c.expression));
			});
			AddCommand("log", (digest,  io) => Log(digest.String()));
			AddCommand("echo", (digest, io) => Log(digest.String()));
			AddCommand("commands", (digest, io) =>
			{
				io.output.AddRange(_commands.Values.OrderBy(c => c.expression));
				// for (int index = 0; index < orderedCommands.Count; index++)
				// {
				// 	Command cmd = orderedCommands[index];
				// 	Log($"{index} - {cmd.expression}");
				// }
			});
			AddCommand("encounters", (digest, io) =>
			{
				io.output.AddRange(_commands.Cast<object>());
			});
			AddCommand("spawns", (digest, io) =>
			{
				io.output.AddRange(SpawnPoint.allActive);
			});
			AddCommand("crystals", (digest, io) =>
			{
				io.output.AddRange(SavePoint.allLoaded);
			});
			AddCommand("trigger", (digest, io) =>
			{
				var ioEncounters = io.input.OfType<EncounterMonster>().ToList();
				if (io.input.Count > 0)
				{
					EncounterMonster encounter = ioEncounters.FirstOrDefault();
					encounter.Trigger(EncounterAdvantages.Neutral);
				}
				else
				{
					throw new ArgumentException("Must be used with a EncounterMonster output. (e.g. `encounters` command)");
				}
			});
			AddCommand("goto", (digester, io) =>
			{
				string name = digester.String().ToLower();
				foreach (LevelManifest manifest in LevelManifestDatabase.LoadedDB.Manifests)
				{
					if (manifest.DisplayName.ToLower().Contains(name))
					{
						GameController.Live.ChangeLevel(manifest);
						break;
					}
				}
			});

			AddCommand("trigger-encounter", (digest, io) =>
			{
				int id = digest.Int(0);
				// EncounterAdvantages advantage = parser.Enum(EncounterAdvantages.Neutral);

				if (id > -1)
				{
					EncounterSpawner.SpawnedEncounter? encounter = EncounterSpawner.All.SafeGet(id)?.spawnedMonsters.FirstOrDefault(mo => mo.encounter.spawned);
					encounter?.encounter.Trigger(EncounterAdvantages.Neutral);
				}
				else
				{
					foreach (EncounterSpawner spawner in EncounterSpawner.All)
					{
						foreach (EncounterSpawner.SpawnedEncounter monster in spawner.spawnedMonsters)
						{
							if (monster.encounter.spawned)
							{
								monster.encounter.Trigger(EncounterAdvantages.Neutral);
								return;
							}
						}
					}
				}

				Log("No encounter spawners");
			});

			AddCommand("despawn", () => GameController.Live.DespawnToSpawnMenu());
			AddCommand("goto-map", () => GameController.Live.ExitGameplayToMenu(GameAssets.Live.DebugLevelSelectMenuScene));
		}

		private void Clear()
		{
			_buffer.Clear();
		}

		private void DrawScene(LogData entry)
		{
			var scene = (Scene) entry.data;
			g.PushID(scene.name);

			if (g.TreeNode($"scene: {scene.name}"))
			{
				// g.Indent();

				GameObject[] roots = scene.GetRootGameObjects();
				for (var index = 0; index < roots.Length; index++)
				{
					GameObject root = roots[index];
					DrawObject(new LogData {index = index, data = root});
				}

				g.TreePop();
			}

			// DrawPropertyTree();
			g.PopID();
		}

		private static void DrawObject(LogData log)
		{
			var go = (GameObject) log.data;
			if (g.TreeNode($"{log.Prefix}{go.name} (game object)"))
			{
				g.Text("Children...");

				for (var i = 0; i < go.transform.childCount; i++)
				{
					Transform child = go.transform.GetChild(i);
					g.Text($"-- Child: {child.name}");
				}

				g.Text("Components...");
				foreach (MonoBehaviour mb in go.GetComponents<MonoBehaviour>())
				{
					// DrawOdin(new LogData
					// {
					// data = mb
					// });
				}

				g.TreePop();
			}
		}

#if UNITY_EDITOR
		private static void DrawProperty(InspectorProperty property)
		{
			if (property == null || property.ValueEntry == null) return;

			g.PushID(property.Name);
			g.PushItemWidth(175);
			switch (property.ValueEntry.WeakSmartValue)
			{
				case float floatValue:
				{
					if (g.DragFloat(property.NiceName, ref floatValue))
						property.ValueEntry.WeakSmartValue = floatValue;
					break;
				}
				case bool bval:
				{
					if (g.Checkbox(property.NiceName, ref bval))
						property.ValueEntry.WeakSmartValue = bval;
					break;
				}
				case Vector2 vec2:
				{
					if (g.DragFloat2(property.NiceName, ref vec2))
						property.ValueEntry.WeakSmartValue = vec2;
					break;
				}
				case Vector3 vec3:
				{
					if (g.DragFloat3(property.NiceName, ref vec3))
						property.ValueEntry.WeakSmartValue = vec3;
					break;
				}
				case Quaternion quat:
				{
					var v4 = new Vector4(quat.x, quat.y, quat.z, quat.w);
					if (g.DragFloat4(property.NiceName, ref v4))
					{
						quat.x                             = v4.x;
						quat.y                             = v4.y;
						quat.x                             = v4.z;
						quat.w                             = v4.w;
						property.ValueEntry.WeakSmartValue = quat;
					}

					break;
				}
				case Color col:
				{
					Vector4 v4 = col.ToV4();
					if (g.ColorPicker4(property.NiceName, ref v4))
						property.ValueEntry.WeakSmartValue = new Color(v4.x, v4.y, v4.z, v4.w);
					break;
				}
				case int intValue:
				{
					if (g.DragInt(property.NiceName, ref intValue))
						property.ValueEntry.WeakSmartValue = intValue;
					break;
				}
				case string stringValue:
				{
					if (g.InputText(property.NiceName, ref stringValue, 1024))
						property.ValueEntry.WeakSmartValue = stringValue;
					break;
				}
				case IList l:
				{
					if (g.TreeNode(property.NiceName))
					{
						for (var i = 0; i < l.Count; i++)
						{
							InspectorProperty elemProp = property.Children[i];

							if (elemProp.Children.Count > 0)
							{
								if (g.CollapsingHeader($"Element {i}"))
								{
									g.Indent();
									foreach (InspectorProperty child in elemProp.Children)
									{
										DrawProperty(child);
									}

									g.Unindent();
								}
							}
							else
							{
								DrawProperty(elemProp.Children[i]);
							}
						}

						g.TreePop();
					}

					break;
				}
				default:
				{
					// g.Text(property.NiceName.PadLeft(2 * property.RecursiveDrawDepth, '-'));
					if (property.Children.Count > 0)
					{
						if (property.Parent == null && g.TreeNode(property.NiceName))
						{
							g.Indent();
							foreach (InspectorProperty child in property.Children)
							{
								DrawProperty(child);
							}

							g.Unindent();
							g.TreePop();
						}
					}
					else
					{
						g.Text(property.NiceName);
					}

					break;
				}
			}

			g.PopID();
		}

		private static void DrawOdin(LogData log)
		{
			if (log.data is PropertyTree tree)
			{
				g.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 0.6f));
				if (g.TreeNode(log.Prefix + (log.text ?? tree.RootProperty.ValueEntry.WeakSmartValue.ToString())))
				{
					g.Indent();
					DrawProperty(tree.RootProperty);
					g.Unindent();
					g.TreePop();
				}

				g.PopStyleColor();
			}
		}
#endif

		public void DrawBoxedMessage(LogData log)
		{
			var data = log.data as string;

			if (g.CollapsingHeader(log.text))
			{
				if (log.color != null)
					g.TextColored(log.color.Value, log.text);
				else
					g.Text(data);
			}
		}

		public void DrawList(LogData log)
		{
			var elements = log.data as IList;

			int n   = elements.Count;
			int pad = n.ToString().Length;

			g.PushStyleColor(ImGuiCol.Text, new Vector4(0.6f, 0.6f, 0.6f, 0.6f));
			for (var i = 0; i < elements.Count; i++)
			{
				object element = elements[i];
				string elemString;

				// if (to_string != null)
				// {
				// elemString = to_string(element);
				// }
				// else

				if (Live._objectFormatters.TryGetValue(element.GetType(), out ObjectFormatter str))
					elemString = str(log);
				else
					elemString = element.ToString();


				g.Text($"{i.ToString().PadLeft(pad, ' ')}: {elemString}");
			}

			g.PopStyleColor();
		}

		private void DrawCommand(LogData obj)
		{
			var command = (Command) obj.data;
			if (g.TreeNode(command.expression))
			{
				g.TreePop();
			}
		}

		public static void AddObjectDrawer<TObject>(ObjectDrawer drawer)
		{
			Live._objectDrawers[typeof(TObject)] = drawer;
		}

		public static void AddObjectToString<TObject>(Func<TObject, string> tostring)
		{
			Live._objectFormatters[typeof(TObject)] = o => tostring((TObject) o.data);
		}

		private void OnEnable()
		{
			DebugSystem.onLayout += OnLayout;
		}

		private void OnDisable()
		{
			DebugSystem.onLayout -= OnLayout;
		}

		private void LateUpdate()
		{
			Keyboard keyb = Keyboard.current;
			if (keyb == null)
				return;

			// Toggle the open state
			if (!Opened && keyb.backslashKey.wasPressedThisFrame)
			{
				Opened = true;
			}

			// Change the opacity
			const float speed = 0.015f;
			if (keyb.pageDownKey.isPressed)
			{
				_opacity -= speed;
			}
			else if (keyb.pageUpKey.isPressed)
			{
				_opacity += speed;
			}

			_muteTimer -= Time.deltaTime;
		}

		private void OnLayout(ref DebugSystem.State state)
		{
			if (!_opened)
				return;

			Vector2 screen = g.GetIO().DisplaySize;
			var     size   = new Vector2(screen.x, screen.y / 2f);

			// WINDOW SETUP
			g.SetNextWindowBgAlpha(_opacity);
			g.SetNextWindowPos(new Vector2(0, screen.y / 2f), ImGuiCond.Always);
			g.SetNextWindowSize(size, ImGuiCond.Always);
			g.SetNextWindowFocus();
			g.SetNextWindowContentSize(size);

			if (g.Begin("Console", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoSavedSettings | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove | ImGuiWindowFlags.NoScrollbar))
			{
				Vector2 windowPadding = g.GetStyle().WindowPadding;
				Vector2 consoleSize   = g.GetContentRegionAvail() - new Vector2(0, g.GetTextLineHeightWithSpacing()) - windowPadding;
				float   inputSize     = g.GetContentRegionAvail().x - windowPadding.x;

				// HEADER
				// ----------------------------------------
				// if (g.BeginMenuBar())
				// {
				//
				// 	if (g.BeginMenu("Categories"))
				// 	{
				// 		g.Selectable("Info");
				// 		g.Selectable("Warning");
				// 		g.Selectable("Error");
				// 		g.Selectable("Exceptions");
				// 		g.Selectable("Asserts");
				// 		// TODO custom categories
				// 		// g.Separator();
				// 		g.EndMenu();
				// 	}
				//
				// 	if (g.BeginMenu("Options"))
				// 	{
				// 		g.DragFloat("Opacity", ref _opacity, 0.03f, 0, 1);
				// 		g.EndMenu();
				// 	}
				//
				// 	if (g.MenuItem("Close"))
				// 	{
				// 		Opened = false;
				// 	}
				//
				// 	g.EndMenuBar();
				// }


				// MESSAGE LISt
				// ----------------------------------------
				g.PushStyleVar(ImGuiStyleVar.ItemSpacing, new Vector2(4, 1));
				if (g.BeginChild("console_pane", consoleSize, true, ImGuiWindowFlags.NoNav | ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoBringToFrontOnFocus))
				{
					foreach (LogData msg in _buffer)
					{
						if (msg.category != null && _filters.TryGetValue(msg.category, out bool isFiltered) && isFiltered)
							// Filtered by category
							continue;

						if (msg.drawer != null)
						{
							msg.drawer(msg);
						}
						else
						{
							if (msg.color.HasValue)
								g.TextColored(msg.color.Value, msg.ToString());
							else
								g.Text(msg.ToString());
						}

						// using (ug.style.text_color(new Color(1.0f, 0.4f, 0.4f), msg.type == LogType.Error || msg.exception != null))
						// using (ug.style.text_color(new Color(1.0f, 0.8f, 0.6f), msg.type == LogType.Warning))
						// {
						// }
					}

					if (_scrollToEnd && _lastScrollMax != g.GetScrollMaxY())
					{
						g.SetScrollY(g.GetScrollMaxY());

						_lastScrollMax = g.GetScrollMaxY();
						_scrollToEnd   = false;
					}

					g.EndChild();
				}

				g.PopStyleVar();


				// TEXT INPUT
				// ----------------------------------------

				unsafe
				{
					// if (g.IsWindowAppearing() || _reclaimFocus)
					// {
					// 	g.SetItemDefaultFocus();
					// 	g.SetKeyboardFocusHere();
					// 	_reclaimFocus = false;
					// }

					// g.SetCursorPos(new Vector2(windowPadding.x, g.GetWindowSize().y - g.GetTextLineHeightWithSpacing() - 8));
					g.PushItemWidth(inputSize);

					ImGuiInputTextFlags flags = ImGuiInputTextFlags.AllowTabInput | ImGuiInputTextFlags.CallbackCharFilter | ImGuiInputTextFlags.CallbackHistory | ImGuiInputTextFlags.CallbackCompletion | ImGuiInputTextFlags.EnterReturnsTrue;
					if (g.InputText("##console_entry", ref _textInput, 1024, flags, OnKeyTyped))
						ConfirmInput();

					g.PopItemWidth();
				}


				// KEYS
				// ----------------------------------------
				if (g.IsKeyPressed((int) Key.Backslash)) Opened = false;
				if (g.IsKeyPressed((int) Key.Escape))
				{
					if (_textInput.Length == 0)
						Opened = false;
					else
						_textInput = "";

					Opened = false;
				}

				// if (g.IsKeyPressed((int) Key.Slash) || g.IsKeyPressed((int) Key.Semicolon) && g.IsKeyDown((int) Key.LeftShift))
				// {
				// 	// g.SetKeyboardFocusHere(0);
				// }

				// using (ug.Split(1, "filter_area"))
				// {
				// 	g.Text("Categories");
				//
				// 	foreach (string category in Loggers.All)
				// 	{
				// 		if (!_filters.ContainsKey(category))
				// 			_filters.Add(category, false);
				//
				// 		bool isChecked = !_filters[category];
				//
				// 		if (g.Checkbox(category, ref isChecked))
				// 		{
				// 			_filters[category] = !isChecked;
				// 		}
				// 	}
				// }
				g.End();
			}
		}

		private unsafe int OnKeyTyped(ImGuiInputTextCallbackData* data)
		{
			switch (data->EventKey)
			{
				case ImGuiKey.UpArrow:
					_reclaimFocus = true;
					ScrollEntryHistory(1);
					return 1;

				case ImGuiKey.DownArrow:
					_reclaimFocus = true;
					ScrollEntryHistory(-1);
					return 1;
			}

			switch (data->EventChar)
			{
				case (ushort) KeyCode.Backslash:
					Opened = false;
					return 1;
			}


			return 0;
		}

		private void ConfirmInput()
		{
			SendCommand(_textInput);

			_entryHistory.Add(_textInput);
			_entryNavIndex = 0;

			_textInput    = "";
			_scrollToEnd  = true;
			_reclaimFocus = true;
		}

		private void ScrollEntryHistory(int off)
		{
			_entryNavIndex = Mathf.Clamp(_entryNavIndex + off, -1, _buffer.Count - 1);
			_textInput     = _entryHistory.SafeGet(_entryHistory.Count - 1 - _entryNavIndex) ?? "";
		}

		public void SendCommand(string inputExpression = null)
		{
			var input  = new List<object>();
			var output = new List<object>();

			Log($":{inputExpression}", true);

			string[] commandChain = inputExpression.Split('|').Select(t => t.Trim()).ToArray();
			for (var index = 0; index < commandChain.Length; index++)
			{
				string parts = commandChain[index];
				_digester.Reset(parts); // Prepare the string digester for this command


				// Select a value in the output list
				// ----------------------------------------
				if (char.IsNumber(_digester.Peek()))
				{
					object sel = output[_digester.Int()];
					output.Clear();
					output.Add(sel);

					if (index == parts.Length - 1)
					{
						// Used as the last pipe in the chain
						LogObject(sel);
					}

					continue;
				}


				// Execute a command on the output
				// ----------------------------------------
				string inputCommand = _digester.Word();


				var foundCommand = false;
				foreach ((string cmd, Command command) in _commands)
				{
					if (cmd == inputCommand)
					{
						try
						{
							input.Clear();
							input.AddRange(output);

							command.execute(_digester, new CommandIO(input, output));
						}

						catch (CommandSyntaxException syntax)
						{
							Log("Bad argument syntax.", color: Color.red);
							Log(command.expression, color: Color.gray);
						}
						catch (Exception e)
						{
							Log(e.Message);
							Log(e.StackTrace, color: Color.red);
							LogSeparator();
							throw;
						}

						foundCommand = true;
						break;
					}
				}


				// No command found.
				// ----------------------------------------
				if (!foundCommand)
				{
					Log($"unknown command '{inputCommand}'", color: Color.red);
					return;
				}
			}


			// Log the output
			// ----------------------------------------
			if (output.Count > 0)
			{
				LogObjects(output);
			}
		}

		public static void Log(string message, bool timestamp = false, Color? color = null)
		{
			Log(null, message, timestamp, color);
		}

		public static void Log(string category, string message, bool timestamp = false, Color? color = null)
		{
			if (!Application.isPlaying || Live == null || !Live._init) return;

			Live._buffer.Add(new LogData(category, message, null, color: color));
			Live._scrollToEnd = true;
		}

		public static void Log<TElem>(IList<TElem> elements, Func<TElem, string> to_string = null)
		{
			if (!Application.isPlaying) return;

			Live._buffer.Add(new LogData
			{
				drawer = Live.DrawList,
				data   = elements
			});
			Live._scrollToEnd = true;
		}

		public static void LogObject(object obj)
		{
			Type type = obj.GetType();

			if (Live._objectDrawers.TryGetValue(type, out ObjectDrawer drawer))
			{
				Live._buffer.Add(new LogData(null, null, null, drawer, obj));
				return;
			}

#if UNITY_EDITOR
			var tree = PropertyTree.Create(obj);
			Live._buffer.Add(new LogData {drawer = DrawOdin, data = tree});
			Live._scrollToEnd = true;
#else
			Live._buffer.Add(new LogData {text = obj.ToString()});
			Live._scrollToEnd = true;
#endif
		}

		public static void LogObjects<TElem>(List<TElem> elements)
		{
			// int n   = elements.Count;
			// int pad = n.ToString().Length;
			for (var index = 0; index < elements.Count; index++)
			{
				LogObject(elements[index]);
			}
		}

		private static void LogSeparator() { }

		private class Command
		{
			public string                            name;
			public string                            expression;
			public Action<StringDigester, CommandIO> execute;

			public override string ToString()
			{
				return expression;
			}
		}

		public struct LogData
		{
			public int?         index;
			public string       category;
			public string       text;
			public Exception    exception;
			public ObjectDrawer drawer;
			public object       data;
			public Color?       color;

			private string _prefix;

			public string Prefix => index.HasValue ? $"{index.Value}. " : "";

			public LogData(string category, string text, Exception exception, ObjectDrawer drawer = null, object data = null, Color? color = null)
			{
				index          = null;
				this.category  = category;
				this.text      = text;
				this.exception = exception;
				this.drawer    = drawer;
				this.data      = data;
				this.color     = color;

				_prefix = null;
				// Time           = withTimestamp ? DateTime.Now : (DateTime?) null;
			}

			public override string ToString()
			{
				var sb = new StringBuilder();

				// string category = string.IsNullOrEmpty(this.category) ? "" : $"[{this.category}]";

				// if (Time != null) sb.Append($"[{Time:HH:mm:ss}] ");
				// if (this.category != null) sb.Append($"{category} ");
				if (text != null) sb.Append($"{text} ");
				if (exception != null) sb.Append($"{Environment.NewLine}- Exception ocurred: {exception}");

				return sb.ToString();
			}
		}
	}

	public readonly struct CommandIO
	{
		public readonly List<object> input;
		public readonly List<object> output;

		public CommandIO(List<object> input, List<object> output)
		{
			this.input  = input;
			this.output = output;
		}
	}
}