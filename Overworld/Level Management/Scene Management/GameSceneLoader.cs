using System;
using System.Collections.Generic;
using System.Text;
using Anjin.Scripting;
using Anjin.Util;
using ImGuiNET;
using Pathfinding.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;
using Util;
using Guid = System.Guid;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;

#endif


namespace Anjin.Nanokin.SceneLoading
{
	public struct InstructionStatus
	{
		public enum State
		{
			Invalid,
			Init,
			Processing,
			Done,
			Error,
		}

		public State state;
		public float Progress;

		public bool IsDoneOrError => state == State.Done ||
		                             state == State.Error ||
		                             state == State.Invalid;

		public InstructionStatus(int progress)
		{
			state    = State.Init;
			Progress = progress;
		}

		public static InstructionStatus InitStatus = new InstructionStatus {state = State.Init};
		public static InstructionStatus NullStatus = new InstructionStatus {state = State.Invalid};
	}

	[DefaultExecutionOrder(-100)]
	public class GameSceneLoader : StaticBoy<GameSceneLoader>, IDebugDrawer
	{
		public delegate bool RefFunc<T, out Boolean>(ref T op);

		//	Vars
		//---------------------------------------------------------------------
		public Scene GlobalScene;
		public bool  PauseOperations;

		[NonSerialized] public ObjectPoolNoFactory<SceneGroup> GroupPool;

		public static List<Operation>                    activeOperations;
		public static Queue<Instruction>                 instructions;
		public static Dictionary<int, InstructionStatus> statuses;
		public static List<SceneGroup>                   loaded_groups;
		public static Dictionary<int, SceneGroup>        ids_to_groups;

		static RefFunc<Operation, bool>[] load_steps;
		static RefFunc<Operation, bool>[] unload_steps;

		static StringBuilder debug_builder;
		static StringBuilder debug_builder2;

		//	Consts
		//---------------------------------------------------------------------
		public const int MAX_GROUPS = 50; //We shouldn't need more than 50 scene groups.


		//	TODO: Events
		//---------------------------------------------------------------------

		//public GameEvent OnSceneGroupLoaded;
		//public GameEvent OnSceneLoaded;
		//public GameEvent OnLevelLoaded;

		//	Unity Functions
		//---------------------------------------------------------------------
		protected override void OnAwake()
		{
			GlobalScene = gameObject.scene;

			debug_builder  = new StringBuilder();
			debug_builder2 = new StringBuilder();

			activeOperations = new List<Operation>();
			instructions     = new Queue<Instruction>();
			statuses         = new Dictionary<int, InstructionStatus>();
			ids_to_groups    = new Dictionary<int, SceneGroup>();
			loaded_groups    = new List<SceneGroup>();

			GroupPool = new ObjectPoolNoFactory<SceneGroup>(MAX_GROUPS, MAX_GROUPS);


			load_steps = new RefFunc<Operation, bool>[]
			{
				step_StartLoadAsync,
				step_Wait,
				step_WaitForLoadAsyncOperations,
				step_FinishLoad
			};

			unload_steps = new RefFunc<Operation, bool>[]
			{
				step_StartUnloadAsync,
				step_WaitForUnloadAsyncOps,
			};
		}

		void Start()
		{
			DebugSystem.Register(this);
		}

		void OnEnable()
		{
			//SceneManager.sceneLoaded += OnSceneLoaded;
		}

		/*void OnSceneLoaded(Scene scene, LoadSceneMode mode)
		{
			Debug.Log(scene.handle);
			Debug.Log(scene.name);
			Debug.Log(scene.path);
			Debug.Log(scene.buildIndex);
			Debug.Log(scene.isDirty);
			Debug.Log(scene.isLoaded);
			Debug.Log(scene.rootCount);
			Debug.Log(scene.isSubScene);
		}
*/

		void Update()
		{
			if (!PauseOperations)
			{
				//If we have any requests pending, process them
				while (instructions.Count > 0)
				{
					var op = new Operation(instructions.Dequeue());
					activeOperations.Add(op);
				}

				for (int i = 0; i < activeOperations.Count; i++)
				{
					Operation op = activeOperations[i];
					if (op.Update())
					{
						op.Dispose();
						activeOperations.RemoveAt(i);
						i--;
					}
					else
					{
						activeOperations[i] = op;
					}
				}
			}

			ids_to_groups.Clear();
			for (int i = 0; i < loaded_groups.Count; i++)
			{
				if (loaded_groups[i].ID.HasValue)
					ids_to_groups[loaded_groups[i].ID.Value] = loaded_groups[i];
			}

			//Prune any empty groups.
			for (int i = 0; i < loaded_groups.Count; i++)
			{
				if (loaded_groups[i].LoadedScenes.Count <= 0)
					loaded_groups.RemoveAt(i--);
			}
		}


		//	API
		//---------------------------------------------------------------------

		public static SceneLoadHandle LoadScene(SceneReference scene, int? set_active = null, SceneGroup parent = null)
		{
			var ins = new Instruction(InstructionType.LoadScenes);
			ins.load_scenes.Add(scene);
			ins.load_set_active_index = set_active;
			ins.parent_group          = parent;

			LogDebug("LoadScene: ", scene.SceneName);

			return SubmitLoad(ins);
		}

		public static SceneLoadHandle LoadFromManifest(LevelManifest manifest, int? set_active = 0, bool load_battle_scene = false, SceneGroup parent = null)
		{
			var ins = new Instruction(InstructionType.LoadManifest);
			ins.load_scenes.Add(manifest.MainScene);
			ins.load_scenes.AddRange(manifest.SubScenes);
			ins.parent_group = parent;

			/*if(load_battle_scene)
				ins.load_scenes.Add(manifest.DefaultBattleArenaScene);*/

			ins.load_set_active_index = set_active;

			LogDebug("LoadFromManifest: ", manifest.name);

			return SubmitLoad(ins);
		}

		public static SceneLoadHandle LoadGroup(SceneReference main_scene, params SceneReference[] sub_scenes) => LoadGroup(main_scene, null, sub_scenes);

		public static SceneLoadHandle LoadGroup(SceneReference main_scene, SceneGroup parent = null, params SceneReference[] sub_scenes)
		{
			var ins = new Instruction(InstructionType.LoadScenes);
			ins.load_scenes.Add(main_scene);
			ins.load_scenes.AddRange(sub_scenes);
			ins.parent_group = parent;

			LogDebug("LoadGroup: ", main_scene.SceneName);
			for (int i = 0; i < sub_scenes.Length; i++)
			{
				LogDebug("- ", sub_scenes[i].SceneName);
			}

			return SubmitLoad(ins);
		}

		public static SceneUnloadHandle UnloadGroups(params SceneLoadHandle[] handles)
		{
			var ins = new Instruction(InstructionType.UnloadGroup);
			LogDebug("UnloadGroups: ");
			for (int i = 0; i < handles.Length; i++)
			{
				var group = handles[i].LoadedGroup;

				if (statuses.ContainsKey(handles[i].ID))
					statuses.Remove(handles[i].ID);

				if (group != null) ins.unload_groups.Add(group);

				LogDebug("- ", group.ID.ToString());
			}

			return SubmitUnload(ins);
		}

		public static SceneUnloadHandle UnloadGroups(params SceneGroup[] groups)
		{
			var ins = new Instruction(InstructionType.UnloadGroup);
			ins.unload_groups.AddRange(groups);

			LogDebug("UnloadGroups: ");

			for (int i = 0; i < groups.Length; i++)
			{
				if (groups[i].ID.HasValue && statuses.ContainsKey(groups[i].ID.Value))
					statuses.Remove(groups[i].ID.Value);

				LogDebug("- ", groups[i].ID.ToString());
			}

			return SubmitUnload(ins);
		}

		[LuaGlobalFunc("gsl_unload_scenes")]
		public static SceneUnloadHandle UnloadScenes(params Scene[] scenes)
		{
			var ins = new Instruction(InstructionType.UnloadScenes);
			ins.unload_scenes.AddRange(scenes);

			LogDebug("UnloadScenes: ");
			for (int i = 0; i < scenes.Length; i++)
			{
				LogDebug("- ", scenes[i].name);
			}

			return SubmitUnload(ins);
		}

		public static SceneUnloadHandle UnloadLevel(Level level)
		{
			if (level == null) return new SceneUnloadHandle();

			var ins = new Instruction(InstructionType.UnloadLevel);

			if (level.Manifest)
				LogDebug("UnloadLevel: ", level.Manifest.DisplayName);

			for (int i = 0; i < loaded_groups.Count; i++)
			{
				if (loaded_groups[i].Level == level)
				{
					var group = loaded_groups[i];
					if (group.ID.HasValue && statuses.ContainsKey(group.ID.Value))
						statuses.Remove(group.ID.Value);

					return UnloadGroups(loaded_groups[i]);
				}
			}

			UnloadScenes(level.gameObject.scene);

			return SubmitUnload(ins);
		}

		public static void EnsureLevelRegistered(Level level)
		{
			for (int i = 0; i < loaded_groups.Count; i++)
			{
				var grp = loaded_groups[i];
				if (grp.Type == SceneGroupType.Level &&
				    grp.Level == level)
					return;
			}

			for (int i = 0; i < activeOperations.Count; i++)
			{
				if (activeOperations[i].instruction.type == InstructionType.LoadManifest)
					return;
			}

			var group = Live.GroupPool.Get();
			group.Reset();

			group.ID             = 0;
			group.MainSceneIndex = 0;
			group.Level          = level;

			group.LoadedScenes.Add(level.gameObject.scene);

			loaded_groups.Add(group);

			if (level.Manifest)
			{
				group.ManifestOfOrigin = level.Manifest;

				LogDebug("RegisterLevel: ", level.Manifest.DisplayName);

				var sub_scenes = level.Manifest.SubScenes;
				for (int i = 0; i < sub_scenes.Count; i++)
				{
					var scene = SceneManager.GetSceneByName(sub_scenes[i].SceneName);
					if (scene.isLoaded)
					{
						group.LoadedScenes.Add(scene);

						LogDebug("	Add Subscene: ", scene.name);
					}
				}
			}
		}

		static SceneLoadHandle SubmitLoad(Instruction ins)
		{
			LogDebug("SubmitLoad: ", ins.ID.ToString());
			SceneLoadHandle h = new SceneLoadHandle {ID = ins.ID};
			instructions.Enqueue(ins);
			statuses[ins.ID] = InstructionStatus.InitStatus;
			return h;
		}

		static SceneUnloadHandle SubmitUnload(Instruction ins)
		{
			LogDebug("SubmitUnload: ", ins.ID.ToString());
			SceneUnloadHandle h = new SceneUnloadHandle {ID = ins.ID};
			instructions.Enqueue(ins);
			statuses[ins.ID] = InstructionStatus.InitStatus;
			return h;
		}

		//	Load Instructions
		//---------------------------------------------------------------------
		public enum InstructionType
		{
			LoadScenes,
			LoadManifest,

			UnloadScenes,
			UnloadGroup,
			UnloadManifest,
			UnloadLevel,
		}

		public enum InstructionOverallType { Load, Unload, }

		public struct Instruction
		{
			public int             ID;
			public InstructionType type;

			//Can take scenes, or a manifest
			public List<SceneReference> load_scenes;
			public List<Scene>          unload_scenes;
			public List<SceneGroup>     unload_groups;

			public int? load_set_active_index;
			public int? load_main_index;

			public SceneGroup parent_group;

			public Instruction(InstructionType _type)
			{
				type                  = _type;
				load_set_active_index = null;
				load_main_index       = 0;
				parent_group          = null;
				ID                    = Guid.NewGuid().GetHashCode();
				load_scenes           = ListPool<SceneReference>.Claim();
				unload_scenes         = ListPool<Scene>.Claim();
				unload_groups         = ListPool<SceneGroup>.Claim();
			}

			public void Dispose()
			{
				LogDebug("Dispose Instruction: ", ID.ToString());
				if (load_scenes != null) ListPool<SceneReference>.Release(load_scenes);
				if (unload_scenes != null) ListPool<Scene>.Release(unload_scenes);
				if (unload_groups != null) ListPool<SceneGroup>.Release(unload_groups);
			}

			public RefFunc<Operation, Boolean>[] GetSteps()
			{
				switch (type)
				{
					case InstructionType.LoadScenes:
					case InstructionType.LoadManifest:
						return load_steps;
					case InstructionType.UnloadScenes:
					case InstructionType.UnloadManifest:
					case InstructionType.UnloadLevel:
					case InstructionType.UnloadGroup:
						return unload_steps;
				}

				return null;
			}

			public InstructionOverallType OverallType
			{
				get
				{
					switch (type)
					{
						case InstructionType.LoadScenes:
						case InstructionType.LoadManifest:
							return InstructionOverallType.Load;
						default:
							return InstructionOverallType.Unload;
					}
				}
			}
		}


		//	Operations
		//---------------------------------------------------------------------
		public struct Operation
		{
			public Instruction instruction;
			public int         step;
			public float       op_timer;


			//Async operations and the names of the scenes they're loading.
			public List<(AsyncOperation, string)> unity_async_ops;
			public List<Scene>                    loaded_scenes;
			public List<string>                   failed_scenes;


			public Operation(Instruction _instruction)
			{
				instruction     = _instruction;
				step            = 0;
				op_timer        = 0;
				unity_async_ops = ListPool<(AsyncOperation, string)>.Claim();
				loaded_scenes   = ListPool<Scene>.Claim();
				failed_scenes   = ListPool<string>.Claim();
			}

			public bool Update()
			{
				var steps = instruction.GetSteps();

				while (step < steps.Length && steps[step](ref this))
				{
					step++;
				}

				return step >= steps.Length;
			}

			public void Dispose()
			{
				LogDebug("Dispose Operation: ", instruction.ID.ToString());
				ListPool<(AsyncOperation, string)>.Release(unity_async_ops);
				ListPool<Scene>.Release(loaded_scenes);
				ListPool<string>.Release(failed_scenes);
				instruction.Dispose();
			}
		}


		//	Load Steps
		//	- Return true if done
		//---------------------------------------------------------------------


		// Start loading all scenes as instructed.
		bool step_StartLoadAsync(ref Operation op)
		{
			LogDebug("step_StartLoadAsync: ", op.instruction.ID.ToString());

			ref Instruction ins    = ref op.instruction;
			var             status = statuses[ins.ID];

			var scenes = ins.load_scenes;
			if (scenes.Count > 0)
			{
				for (int i = 0; i < scenes.Count; i++)
				{
					if (scenes[i].IsInvalid)
					{
						op.failed_scenes.Add(scenes[i].SceneName);
						continue;
					}

					//So the scene doesn't have to be enabled in the build settings.
#if UNITY_EDITOR
					var new_op = EditorSceneManager.LoadSceneAsyncInPlayMode(scenes[i].DetectAssetPath(), new LoadSceneParameters {loadSceneMode = LoadSceneMode.Additive});
#else
						var new_op = SceneManager.LoadSceneAsync(scenes[i].SceneName, LoadSceneMode.Additive);
#endif

					_op = new_op;

					op.unity_async_ops.Add((new_op, scenes[i].SceneName));
					//op.unity_async_ops.Add(new AsyncOpPair(newOp, scenes[i].sceneName));
				}

				status.state                = InstructionStatus.State.Processing;
				statuses[op.instruction.ID] = status;
			}
			else
			{
				//TODO: Error handling.
			}

			return true;
		}

		AsyncOperation _op;

		//Iterate through all load steps and wait till they're either done or errored out.
		bool step_WaitForLoadAsyncOperations(ref Operation op)
		{
			LogDebug("step_WaitForLoadAsyncOperations: ", op.instruction.ID.ToString(), ", ", op.step.ToString(), ", ", op.unity_async_ops.Count.ToString());

			for (int i = 0; i < op.unity_async_ops.Count; i++)
			{
				(AsyncOperation, string) async  = op.unity_async_ops[i];
				InstructionStatus        status = statuses[op.instruction.ID];

				status.Progress             = async.Item1.progress;
				statuses[op.instruction.ID] = status;

				if (async.Item1.isDone)
				{
					Scene scene = SceneManager.GetSceneByName(op.unity_async_ops[i].Item2);

					if (scene.IsValid())
						op.loaded_scenes.Add(scene);
					else
						Debug.LogError("WARNING: Failed to find loaded scene with the name " + op.unity_async_ops[i].Item2);

					op.unity_async_ops.RemoveAt(i--);
				}
			}

			//If no asyncs existing, proceed
			if (op.unity_async_ops.Count == 0) return true;

			return false;
		}

		//Delay step for testing
		bool step_Wait(ref Operation op)
		{
			op.op_timer += Time.deltaTime;
			if (op.op_timer < 0.5f) return false;
			return true;
		}

		[Button]
		public void TestGet()
		{
			for (int i = 0; i < SceneManager.sceneCount; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				Debug.Log(scene.name + ", " + scene.path + "," + scene.isLoaded);
			}
		}

		//Finalize the load, registering a new SceneGroup so the scene loader can track
		bool step_FinishLoad(ref Operation op)
		{
			LogDebug("step_FinishLoad: ", op.instruction.ID.ToString());

			var group = GroupPool.Get();
			group.Reset();

			group.ID             = op.instruction.ID;
			group.MainSceneIndex = op.instruction.load_main_index;

			group.LoadedScenes.AddRange(op.loaded_scenes);

			if (op.instruction.load_set_active_index != null)
				SceneManager.SetActiveScene(group.LoadedScenes[op.instruction.load_set_active_index.Value]);

			loaded_groups.Add(group);

			if (op.instruction.parent_group != null)
			{
				group.Parent = op.instruction.parent_group;
				op.instruction.parent_group.Children.Add(group);
			}

			var status = statuses[op.instruction.ID];
			status.state                = InstructionStatus.State.Done;
			statuses[op.instruction.ID] = status;

			//Find a level component in the scene if one exists
			for (int i = 0; i < group.LoadedScenes.Count; i++)
			{
				var roots = group.LoadedScenes[i].GetRootGameObjects();

				for (int j = 0; j < roots.Length; j++)
				{
					var lvl = roots[j].GetComponent<Level>();
					if (lvl == null)
						lvl = roots[j].transform.GetComponentInChildren<Level>();

					if (lvl != null)
					{
						group.Level = lvl;
						break;
					}
				}
			}

			return true;
		}

		bool step_StartUnloadAsync(ref Operation op)
		{
			LogDebug("step_StartUnloadAsync: ", op.instruction.ID.ToString());
			if (op.instruction.type != InstructionType.UnloadScenes)
			{
				var groups = op.instruction.unload_groups;

				void start_group_unload(ref Operation _op, SceneGroup grp, out bool removed)
				{
					removed = false;

					if (grp.Parent != null)
					{
						grp.Parent.Children.Remove(grp);
						removed = true;
					}

					foreach (Scene scene in grp.LoadedScenes)
					{
						if (scene.IsValid())
							_op.unity_async_ops.Add((SceneManager.UnloadSceneAsync(scene, UnloadSceneOptions.UnloadAllEmbeddedSceneObjects), scene.name));
					}

					for (int i = 0; i < grp.Children.Count; i++)
					{
						start_group_unload(ref _op, grp.Children[i], out bool _removed);
						if (_removed) i--;
					}
				}

				foreach (SceneGroup grp in groups)
				{
					start_group_unload(ref op, grp, out bool removed);
					loaded_groups.Remove(grp);
				}
			}
			else
			{
				var scenes = op.instruction.unload_scenes;
				for (int i = 0; i < scenes.Count; i++)
				{
					if (scenes[i].IsValid())
						op.unity_async_ops.Add((SceneManager.UnloadSceneAsync(scenes[i], UnloadSceneOptions.UnloadAllEmbeddedSceneObjects), scenes[i].name));

					for (int j = 0; j < loaded_groups.Count; j++)
					{
						if (loaded_groups[j].LoadedScenes.Contains(scenes[i]))
							loaded_groups[j].LoadedScenes.Remove(scenes[i]);
					}
				}
			}

			return true;
		}

		bool step_WaitForUnloadAsyncOps(ref Operation op)
		{
			LogDebug("step_WaitForUnloadAsyncOps: ", op.instruction.ID.ToString());

			for (int i = 0; i < op.unity_async_ops.Count; i++)
			{
				if (op.unity_async_ops[i].Item1 == null)
				{
					// oxy:
					// I commented this out because it seems we are still having trouble with this
					// on occasions, and the console spam tanks the performances
					// I really think we should update this class to use async, it would simplify things massively

					// Debug.Log($"Async operation is null for {op.unity_async_ops[i].Item2}");
					continue;
				}

				if (op.unity_async_ops[i].Item1.isDone)
					op.unity_async_ops.RemoveAt(i--);
			}

			// If no asyncs existing, proceed
			if (op.unity_async_ops.Count == 0)
			{
				var status = statuses[op.instruction.ID];
				status.state                = InstructionStatus.State.Done;
				statuses[op.instruction.ID] = status;
				return true;
			}

			return false;
		}

		static int debug_lines = 0;

		static void LogDebug(params string[] s)
		{
			if (debug_lines > 5000)
			{
				debug_builder.Clear();
				debug_lines = 0;
			}

			//debug_builder2.Clear();
			for (int i = 0; i < s.Length; i++)
			{
				debug_builder.Append(s[i]);
				//debug_builder2.Append(s[i]);
			}

			debug_lines++;

			debug_builder.AppendLine();

			//Debug.Log(debug_builder2.ToString());
		}

		[DebugRegisterGlobals]
		public static void RegisterMenu()
		{
			DebugSystem.RegisterMenu("Scene Loader");
		}

		void AutoSizeColumn(string text, ref float currWidth)
		{
			var size = ImGui.CalcTextSize(text).x;
			if (size > currWidth)
			{
				ImGui.SetColumnWidth(-1, size);
				currWidth = size;
			}
		}

		static Vector4 Col1 = Color.HSVToRGB(0, 0.0f, 0.7f);
		static Vector4 Col2 = Color.HSVToRGB(0, 0.0f, 0.9f);

		public void OnLayout(ref DebugSystem.State state)
		{
			if (state.IsMenuOpen("Scene Loader"))
			{
				var log_h = 186;

				if (ImGui.Begin("GameSceneLoader"))
				{
					var wh = ImGui.GetWindowHeight() - 70;

					ImGui.Text("Operations:");

					if (ImGui.BeginChild("operations_view", new Vector2(0, wh - log_h), false, ImGuiWindowFlags.AlwaysAutoResize))
					{
						for (int i = 0; i < activeOperations.Count; i++)
						{
							var op  = activeOperations[i];
							var ins = op.instruction;

							ImGui.PushID(i);

							ImGui.BeginGroup();
							{
								ImGui.Columns(4, "vars", true);

								ImGui.Text("ID:");
								ImGui.Text(ins.ID.ToString());
								ImGui.NextColumn();
								ImGui.Text("Type:");
								ImGui.Text(ins.type.ToString());
								ImGui.NextColumn();
								ImGui.Text("Main Index:");
								ImGui.Text(ins.load_main_index.ToString());
								ImGui.NextColumn();
								ImGui.Text("Set Active Index:");
								ImGui.Text(ins.load_set_active_index.ToString());

								ImGui.Columns(1);
							}
							ImGui.EndGroup();

							ImGui.PopID();
						}

						if (activeOperations.Count > 0)
							ImGui.Separator();

						ImGui.TextColored(Color.yellow.ToV4(), "Loaded Groups:");

						for (int i = 0; i < loaded_groups.Count; i++)
						{
							var grp = loaded_groups[i];

							ImGui.PushID(i);
							ImGui.Separator();
							ImGui.BeginGroup();
							{
								ImGui.Columns(2, "_vars", true);
								{
									ImGui.Text("ID: " + grp.ID);
									ImGui.Text("Main Index:" + grp.MainSceneIndex);
									ImGui.Text("Level: " + (grp.Level ? grp.Level.ToString() : "null"));

									ImGui.NextColumn();

									var sz = new Vector2(0, 16);

									if (ImGui.Button("Unload", sz)) UnloadGroups(grp);
									ImGui.SameLine();
									if (ImGui.Button("Set Main Scene Active", sz)) grp.SetMainSceneActive();

									ImGui.Text("Set Root Objs: ");
									ImGui.SameLine();
									if (ImGui.Button("Active", sz)) grp.SetRootObjectsActive(true);
									ImGui.SameLine();
									if (ImGui.Button("InActive", sz)) grp.SetRootObjectsActive(false);
								}
								ImGui.Columns(1);

								ImGui.TextColored(Color.yellow.ToV4(), "Scenes:");

								ImGui.Indent(16);

								ImGui.BeginGroup();
								{
									for (int j = 0; j < grp.LoadedScenes.Count; j++)
									{
										var scene = grp.LoadedScenes[j];

										ImGui.BeginGroup();
										{
											if (scene.IsValid())
											{
												ImGui.Text(scene.name);
												ImGui.Text(scene.path);

												ImGui.TextColored(Col1, "Handle: " + scene.handle);
												AImgui.HSpacer(16);
												ImGui.TextColored(Col2, "Build Ind: " + scene.buildIndex);
												AImgui.HSpacer(16);
												ImGui.TextColored(Col1, "Valid: " + scene.IsValid());
												AImgui.HSpacer(16);
												ImGui.TextColored(Col2, "Loaded: " + scene.isLoaded);
												AImgui.HSpacer(16);
												ImGui.TextColored(Col1, "Dirty: " + scene.isDirty);

												ImGui.TextColored(Col2, "Root Cnt: " + scene.rootCount);
												AImgui.HSpacer(16);
												ImGui.TextColored(Col1, "Is Sub: " + scene.isSubScene);
											}
											else
											{
												ImGui.TextColored(Color.red, "INVALID SCENE");
											}
										}
										ImGui.EndGroup();
									}
								}
								ImGui.EndGroup();

								ImGui.Unindent(16);
							}
							ImGui.EndGroup();

							ImGui.Separator();
							ImGui.Dummy(new Vector2(0, 16));

							ImGui.PopID();
						}
					}

					ImGui.EndChild();


					ImGui.Text("Log:");
					if (ImGui.BeginChild("debug_output", new Vector2(0, log_h), true))
					{
						ImGui.TextUnformatted(debug_builder.ToString());
					}

					ImGui.EndChild();
				}

				ImGui.End();
			}
		}
	}
}