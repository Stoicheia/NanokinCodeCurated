using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;

#endif

namespace Anjin.Nanokin.TEMP
{
	/// <summary>
	/// Provides mechanisms to easily control complex scene loading.
	/// </summary>
	public class NanokinSceneLoader : StaticBoy<NanokinSceneLoader>
	{
		// Keep a reference to this around just in case we need it for some reason
		[NonSerialized]
		public Scene GlobalScene;

		[BoxGroupExt("GameScenes", 0.3f, 0.6f, 0.9f), HideInEditorMode]
		public List<GameScene> LoadedGameScenes;
		[BoxGroupExt("GameScenes"), HideInEditorMode]
		public Dictionary<string, GameScene> NamesToGameScenes;
		[BoxGroupExt("GameScenes"), HideInEditorMode]
		public Dictionary<Scene, GameScene> ScenesToGameScenes;

		[BoxGroupExt("GameScenes"), HideInEditorMode]
		public GameScene ActiveSceneHolder;

		[BoxGroupExt("Operations", 0.4f, 0.8f, 0.9f), HideInEditorMode]
		public bool PauseOperations;
		[BoxGroupExt("Operations"), HideInEditorMode]
		public Queue<OpRequest> Requests;
		[BoxGroupExt("Operations"), HideInEditorMode]
		public List<BaseOperation> ActiveOperations;

		public bool IsSceneLoaded(string Name) => NamesToGameScenes.ContainsKey(Name);

		static List<StepBase> loadSteps;
		static List<StepBase> unloadSteps;
		static List<StepBase> changeSteps;

		[FoldoutGroup("Unity Events")] public UnityEvent onGeneralSceneLoaded;

		[FoldoutGroup("Unity Events")] public UnityEvent onBeforeLevelUnloadMono;
		[FoldoutGroup("Unity Events")] public UnityEvent onAfterLevelLoadMono;

		private void Start()
		{
			Requests           = new Queue<OpRequest>();
			ActiveOperations   = new List<BaseOperation>();
			LoadedGameScenes   = new List<GameScene>();
			NamesToGameScenes  = new Dictionary<string, GameScene>();
			ScenesToGameScenes = new Dictionary<Scene, GameScene>();

			SetupSteps();

			// The scene loader should be in the global scene
			GlobalScene = gameObject.scene;
		}

		void SetupSteps()
		{
			//Load (Load a scene)
			loadSteps = new List<StepBase>
			{
				new StartLoadAsyncStep(),
				new WaitForLoadStep(),
				new FinishLoadStep()
			};


			//Unload (Unload a GameScene)
			unloadSteps = new List<StepBase>
			{
				new StartUnloadStep(),
				new WaitForUnloadStep()
			};
		}

		/// <summary>
		/// Gets a flag indicating whether or not the level manager is currently on standby.
		/// </summary>
		public bool IsOnStandby => ActiveOperations.Count == 0;
		/*

			/// <summary>
			/// Gets or sets the current level.
			/// </summary>
			public LevelScene ActiveLevel {
				get => _activeLevel;
				set {
					_activeLevel = value;

					if (_activeLevel != null)
					{
						value.SetAsleep(false);                          // Makes sure the level gameObject is active.
						SceneManager.SetActiveScene(_activeLevel.Scene); // Sets the level scene as the active one, for environment effects.
					}
				}
			}*/

		/*/// <summary>
		/// Finds the active loaded Level.
		/// </summary>
		private LevelScene FindActive()
		{
			Level level = FindObjectOfType<Level>();

			if (level != null)
			{
				Scene scene = level.gameObject.scene;

				LevelScene loadedLevel = new LevelScene(level, scene);
				_loadedLevels[loadedLevel.Scene.name] = loadedLevel;
				return loadedLevel;
			}

			return null;
		}*/

		public static NormalLoadRequest RequestSceneLoad(SceneReference reference)
		{
			var req = new NormalLoadRequest(reference) {SetToActive = true};
			Live.SubmitRequest(req);
			return req;
		}

		public static NormalUnloadRequest RequestSceneUnload(GameScene scene)
		{
			if (Live.LoadedGameScenes.Contains(scene))
			{
				var req = new NormalUnloadRequest(scene);
				Live.SubmitRequest(req);
				return req;
			}

			return null;
		}

		public static LevelManifestLoadRequest RequestLevelManifestLoad(LevelManifest level)
		{
			var req = new LevelManifestLoadRequest(level) {SetToActive = true};
			Live.SubmitRequest(req);
			return req;
		}

		/// <summary>
		/// Requests to unload a level, identified by the level object.
		/// </summary>
		public static LevelUnloadRequest RequestLevelUnload(Level level)
		{
			var req = new LevelUnloadRequest(level);
			Live.SubmitRequest(req);
			return req;
		}

		/// <summary>
		/// Requests to unload a level, identified by the manifest of origin.
		/// </summary>
		public static LevelUnloadRequest RequestLevelUnload(LevelManifest manifest)
		{
			var req = new LevelUnloadRequest(manifest);
			Live.SubmitRequest(req);
			return req;
		}

		void SubmitRequest(OpRequest request)
		{
			Requests.Enqueue(request);
		}

		private void Update()
		{
			if (!PauseOperations)
			{
				//If we have any requests pending, process them
				while (Requests.Count > 0)
				{
					var request = Requests.Dequeue();
					var op      = request.MakeOperation();
					ActiveOperations.Add(op);
				}

				for (int i = 0; i < ActiveOperations.Count; i++)
				{
					ActiveOperations[i].Update();

					if (ActiveOperations[i].Done)
					{
						ActiveOperations.RemoveAt(i);
						i--;
					}
				}
			}
		}

		public void RegisterGameScene(GameScene scene)
		{
			if (!LoadedGameScenes.Contains(scene))
			{
				LoadedGameScenes.Add(scene);
				NamesToGameScenes[scene.LoadedScene.name] = scene;
			}
		}

		void UnregisterGameScene(GameScene scene)
		{
			LoadedGameScenes.Remove(scene);
			NamesToGameScenes.Remove(scene.LoadedScene.name);
		}

		public GameScene GetGameSceneFromObject(GameObject obj)
		{
			for (int i = 0; i < LoadedGameScenes.Count; i++)
			{
				if (LoadedGameScenes[i].LoadedScene == obj.scene || LoadedGameScenes[i].LoadedSubScenes.Contains(obj.scene))
				{
					return LoadedGameScenes[i];
				}
			}

			return null;
		}

		//TODO: Implement
		public List<AsyncOperation> GetCurrentLoadAsyncOperations()
		{
			return null;
		}

	#region Utilities

		public List<Scene> GetAllLoadedScenes()
		{
			List<Scene> loadedScenes = new List<Scene>();

			//Why did they make SceneManager.GetAllScenes() obsolete? WTF
			for (int i = 0; i < SceneManager.sceneCount; i++) loadedScenes.Add(SceneManager.GetSceneAt(i));

			return loadedScenes;
		}

	#endregion

	#region Loading steps.

	#region LOADING STEP BASE

		public abstract class StepBase
		{
			public abstract bool DoStep(BaseOperation op);
		}

		public abstract class Step<OP> : StepBase where OP : BaseOperation
		{
			public override bool DoStep(BaseOperation op)
			{
				return StepUpdate((OP) op);
			}

			public abstract bool StepUpdate(OP op);
		}

		public abstract class LoadStep : Step<LoadOperation>
		{
			public abstract override bool StepUpdate(LoadOperation op);
		}

		public abstract class UnloadStep : Step<UnloadOperation>
		{
			public abstract override bool StepUpdate(UnloadOperation op);
		}

	#endregion


		public class BeforeLoadStep : LoadStep
		{
			public override bool StepUpdate(LoadOperation op)
			{
				// NOTE: We may want to unload a level silently for example without showing this loading screen
				// it seems the loading screen is activated externally through the UnityEvent below, which is inconvenient
				// in this case.
				Live.onBeforeLevelUnloadMono.Invoke();
				return true;
			}
		}

		/// <summary>
		/// Start loading the main and SubScene references
		/// </summary>
		public class StartLoadAsyncStep : LoadStep
		{
			public override bool StepUpdate(LoadOperation op)
			{
				var scenes = op.Request.ScenesToLoad;
				if (scenes.Length > 0)
				{
					var main = scenes[0];
					if (!main.IsInvalid)
					{
						//So the scene doesn't have to be enabled in the build settings.
#if UNITY_EDITOR
						op.AsyncOp = EditorSceneManager.LoadSceneAsyncInPlayMode(main.DetectAssetPath(), new LoadSceneParameters {loadSceneMode = LoadSceneMode.Additive});
#else
							op.AsyncOp = SceneManager.LoadSceneAsync(main.SceneName, LoadSceneMode.Additive);
#endif

						op.AsyncOp.allowSceneActivation = op.AllowSceneActivation;
						op.SceneRef                     = main;
					}

					for (int i = 1; i < scenes.Length; i++)
					{
						if (!scenes[i].IsInvalid)
						{
							//So the scene doesn't have to be enabled in the build settings.
#if UNITY_EDITOR
							var newOp = EditorSceneManager.LoadSceneAsyncInPlayMode(scenes[i].DetectAssetPath(), new LoadSceneParameters {loadSceneMode = LoadSceneMode.Additive});
#else
								var newOp = SceneManager.LoadSceneAsync(scenes[i].SceneName, LoadSceneMode.Additive);
#endif

							op.SubAsyncOps.Add(new AsyncOpPair(newOp, scenes[i].SceneName));
							op.SubSceneRefs.Add(scenes[i]);
						}
					}
				}

				return true;
			}
		}

		public class WaitForLoadStep : LoadStep
		{
			public override bool StepUpdate(LoadOperation op)
			{
				if (op.AsyncOp != null && op.AsyncOp.isDone)
				{
					Live.onGeneralSceneLoaded?.Invoke();
					op.GeneratedScene = SceneManager.GetSceneByName(op.SceneRef.SceneName);
					op.AsyncOp        = null;
				}

				int nameIndex = 0;
				for (int i = 0; i < op.SubAsyncOps.Count; i++)
				{
					if (op.SubAsyncOps[i].AsyncOp.isDone)
					{
						//Register the scene
						op.GeneratedSubScenes.Add(SceneManager.GetSceneByName(op.SubAsyncOps[i].sceneName));
						Live.onGeneralSceneLoaded?.Invoke();
						op.SubAsyncOps.RemoveAt(i);
						i--;
					}

					nameIndex++;
				}

				//No more scenes to wait for
				if (op.AsyncOp == null && op.SubAsyncOps.Count == 0)
				{
					return true;
				}

				return false;

				/*if (AsyncLoadOp == null || AsyncLoadOp.isDone)
				{
					Scene loadedScene = SceneManager.GetSceneByName(TargetScene.sceneName);
					Level loadedLevel = loadedScene.FindRootComponent<Level>();

					_loadedLevels[loadedScene.name] = ActiveLevel = new LevelScene(loadedLevel, loadedScene);
					return true;
				}*/
			}
		}

		public class FinishLoadStep : LoadStep
		{
			public override bool StepUpdate(LoadOperation op)
			{
				op.gameScene = new GameScene(op.GeneratedScene) {LoadedSubScenes = op.GeneratedSubScenes};

				op.Request.Complete(op.gameScene);

				Live.RegisterGameScene(op.gameScene);

				/*if (ActiveLevel.Level != null)
				{
					ActiveLevel.Level.OnLoad();
				}

				onAfterLevelLoadMono.Invoke();
				_onOperationCompleted?.Invoke(ActiveLevel);

				TargetScene = null;*/
				return true;
			}
		}


		public class StartUnloadStep : UnloadStep
		{
			public override bool StepUpdate(UnloadOperation op)
			{
				var scenes = op.Request.GetScenesToUnload();

				for (int i = 0; i < scenes.Length; i++)
				{
					GameScene gs = scenes[i];
					op.SubAsyncOps.Add(new AsyncOpPair(SceneManager.UnloadSceneAsync(gs.LoadedScene), ""));
					for (int j = 0; j < gs.LoadedSubScenes.Count; j++)
					{
						op.SubAsyncOps.Add(new AsyncOpPair(SceneManager.UnloadSceneAsync(gs.LoadedSubScenes[j]), ""));
					}

					Live.UnregisterGameScene(gs);
				}

				/*switch(LoadOption)
				{
					case LoadOptions.Replace:
						if (ActiveLevel != null)
						{
							AsyncUnloadOp = SceneManager.UnloadSceneAsync(ActiveLevel.Scene);
						}

						break;

					case LoadOptions.Preserve:
						ActiveLevel.SetAsleep();
						break;

					default:
						throw new ArgumentOutOfRangeException(nameof(LoadOption), LoadOption, null);
				}*/

				return true;
			}
		}

		public class WaitForUnloadStep : UnloadStep
		{
			public override bool StepUpdate(UnloadOperation op)
			{
				for (int i = 0; i < op.SubAsyncOps.Count; i++)
				{
					if (!op.SubAsyncOps[i].AsyncOp.isDone) return false;
				}

				return true;

				//return AsyncUnloadOp == null || AsyncUnloadOp.isDone;
			}
		}

	#endregion

		/// <summary>
		/// How the Loader should behave when asking the SceneManager to load the scene(s) from the LoadRequest
		/// </summary>
		public enum LoadingAsyncMode
		{
			/// <summary>
			/// Scenes should not use asynchronous loading/unloading, resulting in the game freezing while the scene loads
			/// </summary>
			NoAsync,
			/// <summary>
			/// Scenes should be loaded/unloaded one after another, using the asynchronous methods
			/// </summary>
			Queued,
			/// <summary>
			/// Scenes should be loaded/unloaded side by side, using the asynchronous methods
			/// </summary>
			Concurrent
		}


		/*/// <summary>
		  /// Represents the different kind of loading operations available.
		  /// </summary>
		  public enum LoadOptions
		  {
			  None,
			  UnloadOthers,
			  DisableOthers,
		  }*/

		public struct RequestStatus
		{
			public float Progress; //Normalized 0-1
			public bool  Done;
			public bool  Error;
		}

		interface ILoadRequest
		{
			void GrabScenes(List<SceneReference> scenes);
		}

		enum RequestType
		{
			LoadScenes,
			LoadManifest,
			UnloadScenes,
			UnloadManifest,
			UnloadLevel,
			LoadIntoGroup,
		}

		/// <summary>
		///	Requests the scene loader to start a load operation
		/// </summary>
		struct LoadRequest_
		{
			public RequestType      type;
			public RequestStatus    status;
			public LoadingAsyncMode async_mode;

			//Settings
			public bool set_to_active;
			public int  index_to_set_active;
			public bool allow_scene_activation;

			public List<SceneReference> scenes;
			public LevelManifest        manifest;

			public void GetScenes(List<SceneReference> out_scenes)
			{
				switch (type)
				{
					case RequestType.LoadScenes:
						out_scenes.AddRange(scenes);
						break;

					case RequestType.LoadManifest:
						out_scenes.Add(manifest.MainScene);
						out_scenes.AddRange(manifest.SubScenes);

						//TODO: Make this happen async after level loaded???
						out_scenes.AddRange(manifest.ArenaScenes);

						break;


					case RequestType.UnloadScenes:
						break;

					case RequestType.UnloadManifest:
						break;
				}
			}

			public void OnDone(GameScene scene) { }
		}

		/// <summary>
		/// Any request to the manager to perform an operation
		/// </summary>
		public abstract class OpRequest
		{
			public RequestStatus Status;

			protected OpRequest()
			{
				Status = new RequestStatus();
			}

			public virtual BaseOperation MakeOperation()
			{
				BaseOperation op = new BaseOperation(this);
				return op;
			}

			public virtual void UpdateProgress() { }

			public virtual void OnOpDone()
			{
				Status.Done = true;
			}
		}

		/// <summary>
		/// Defines a request to load any number of scenes, to be given to the LevelLoader.
		/// Can also do things to the GameScene that gets generated afterwards.
		/// </summary>
		public abstract class LoadRequest : OpRequest
		{
			private LoadOperation _operation;

			public event Action<GameScene> Completed;

			/// <summary>
			/// Should one of the scenes in this request be set to active?
			/// </summary>
			public bool SetToActive;

			/// <summary>
			/// Which scene index should we set to active?
			/// </summary>
			public int IndexToSetActive;

			/// <summary>
			/// Should components in the scene be activated by default? (awake and start methods)
			/// Useful for debugging lag problems.
			/// </summary>
			public bool AllowSceneActivation = true;

			/// <summary>
			/// The first scene will be considered the main scene, while subsequent scenes are the SubScenes
			/// </summary>
			/// <value></value>
			public abstract SceneReference[] ScenesToLoad { get; }

			public override BaseOperation MakeOperation()
			{
				_operation = new LoadOperation(this)
				{
					Steps                = loadSteps,
					AllowSceneActivation = AllowSceneActivation
				};
				return _operation;
			}

			public override void UpdateProgress()
			{
				float totalProgress = 0;
				int   numValues     = 0;

				if (_operation.AsyncOp != null)
				{
					totalProgress += _operation.AsyncOp.progress;
					numValues++;
				}

				for (int i = 0; i < _operation.SubAsyncOps.Count; i++)
				{
					if (_operation.SubAsyncOps[i].AsyncOp != null)
					{
						totalProgress += _operation.SubAsyncOps[i].AsyncOp.progress;
						numValues++;
					}
				}

				if (numValues > 0)
				{
					Status.Progress = totalProgress / numValues;
				}
			}

			/// <summary>
			/// Takes in the generated GameScene, so it can be modified further after loading.
			/// </summary>
			/// <param name="gameScene">Should already have the Scene field set.</param>
			public void Complete(GameScene gameScene)
			{
				if (SetToActive)
				{
					if (IndexToSetActive == 0)
						gameScene.SetMainSceneActive();
					else
						gameScene.SetSubSceneActive(IndexToSetActive - 1);
				}

				OnComplete(gameScene);

				Completed?.Invoke(gameScene);
			}

			protected virtual void OnComplete(GameScene gameScene) { }
		}

		/// <summary>
		/// Loads a list of scenes in order, nothing fancy
		/// </summary>
		public class NormalLoadRequest : LoadRequest
		{
			public List<SceneReference> Scenes;

			public NormalLoadRequest()
			{
				Scenes = new List<SceneReference>();
			}

			public NormalLoadRequest(params SceneReference[] scenes) : this()
			{
				Scenes.AddRange(scenes);
			}

			public override SceneReference[] ScenesToLoad
			{
				get { return Scenes.ToArray(); }
			}
		}

		/// <summary>
		/// Loads all scenes in a level manifest.
		/// </summary>
		public class LevelManifestLoadRequest : LoadRequest
		{
			public LevelManifest Manifest;

			public LevelManifestLoadRequest(LevelManifest manifest)
			{
				Manifest = manifest;
			}

			public override SceneReference[] ScenesToLoad
			{
				get
				{
					List<SceneReference> scenes = new List<SceneReference>();
					scenes.Add(Manifest.MainScene);
					scenes.AddRange(Manifest.SubScenes);
					scenes.AddRange(Manifest.ArenaScenes);

					return scenes.ToArray();
				}
			}

			protected override void OnComplete(GameScene gameScene)
			{
				//Try to find the level marker
				Level lvl   = null;
				var   roots = gameScene.LoadedScene.GetRootGameObjects();
				for (int i = 0; i < roots.Length; i++)
				{
					lvl = roots[i].GetComponent<Level>();
					if (lvl == null)
						lvl = roots[i].transform.GetComponentInChildren<Level>();

					if (lvl != null) break;
				}

				//Mark the GameScene so we know it's a level
				gameScene.SetToLevelType(lvl, Manifest);

				Live.onAfterLevelLoadMono.Invoke();
			}
		}

		public abstract class UnloadRequest : OpRequest
		{
			public abstract GameScene[] GetScenesToUnload();

			public LoadingAsyncMode AsyncMode;

			public override BaseOperation MakeOperation()
			{
				return new UnloadOperation(this) {Steps = unloadSteps};
			}
		}

		public class NormalUnloadRequest : UnloadRequest
		{
			public List<GameScene> Scenes;

			public NormalUnloadRequest()
			{
				Scenes = new List<GameScene>();
			}

			public NormalUnloadRequest(params GameScene[] scenes) : this()
			{
				Scenes.AddRange(scenes);
			}

			public override GameScene[] GetScenesToUnload()
			{
				return Scenes.ToArray();
			}
		}

		public class LevelUnloadRequest : UnloadRequest
		{
			public Level         Level;
			public LevelManifest Manifest;

			public LevelUnloadRequest(Level _level)
			{
				Level = _level;
			}

			public LevelUnloadRequest(LevelManifest _manifest)
			{
				Manifest = _manifest;
			}

			public override GameScene[] GetScenesToUnload()
			{
				//Find any GameScenes for the level loaded. We first check for the actual level object, then the manifest.
				//(We probably won't allow multiple copies of the same level, but we should account for it just in case)

				if (Level != null)
				{
					return Live.LoadedGameScenes.FindAll(x => x.Level == Level).ToArray();
				}
				else
				{
					return Live.LoadedGameScenes.FindAll(x => x.ManifestOfOrigin == Manifest).ToArray();
				}
			}
		}

		/// <summary>
		/// Represents a group of a loaded main scene & subscenes that can be tracked by the Level Manager.
		/// Can have a parent and child GameScene, allowing for easily chaining together GameScenes in a hierarchy
		/// </summary>
		public class GameScene
		{
			public enum GameSceneType
			{
				/// <summary>
				/// Just a basic holder for a main scene and sub scenes
				/// </summary>
				Normal,

				/// <summary>
				/// A holder for a level scene and sub scenes.
				/// Generated from when a level manifest is loaded.
				/// </summary>
				Level,
			}

			public GameScene Parent;
			public GameScene Child;

			[ShowInInspector]
			public GameSceneType type { get; private set; }

			public Scene       LoadedScene;
			public List<Scene> LoadedSubScenes;

			public Level         Level;
			public LevelManifest ManifestOfOrigin;

			private GameScene()
			{
				LoadedSubScenes = new List<Scene>();
			}

			/// <summary>
			/// Creates a normal GameScene.
			/// </summary>
			/// <param name="_loadedScene"></param>
			/// <param name="_parent"></param>
			public GameScene(Scene _loadedScene, GameScene _parent = null) : this()
			{
				type        = GameSceneType.Normal;
				LoadedScene = _loadedScene;
				Parent      = _parent;
			}

			/// <summary>
			/// Creates a Level GameScene.
			/// </summary>
			/// <param name="_level"></param>
			/// <param name="_manifest"></param>
			/// <param name="_parent"></param>
			public GameScene(Level _level, LevelManifest _manifest, GameScene _parent = null) : this()
			{
				type = GameSceneType.Level;

				Level = _level;

				if (_level) LoadedScene = _level.gameObject.scene;

				ManifestOfOrigin = _manifest;
				Parent           = _parent;
			}

			public void SetToLevelType(Level _level, LevelManifest _manifest)
			{
				type = GameSceneType.Level;

				Level = _level;
				if (_level) LoadedScene = _level.gameObject.scene;

				ManifestOfOrigin = _manifest;
			}

			public void SetChild(GameScene scene)
			{
				Child        = scene;
				scene.Parent = this;
			}

			public bool SetMainSceneActive()         => SceneManager.SetActiveScene(LoadedScene);
			public bool SetSubSceneActive(int index) => SceneManager.SetActiveScene(LoadedSubScenes[index]);

			/// <summary>
			/// Shortcut for setting every root object in a GameScene active
			/// </summary>
			public void SetRootObjectsActive(bool state = true)
			{
				if (!LoadedScene.IsValid()) return;

				foreach (GameObject rootGameObject in LoadedScene.GetRootGameObjects())
				{
					rootGameObject.SetActive(state);
				}

				/*if (Level != null)
					Level.gameObject.SetActive(!state);*/
			}

			public void AutoCatalogLevelSubscenes()
			{
				if (ManifestOfOrigin != null)
				{
					var allScenes = Live.GetAllLoadedScenes();
					for (int i = 0; i < ManifestOfOrigin.SubScenes.Count; i++)
					{
						var sub = ManifestOfOrigin.SubScenes[i];

						if (allScenes.Any(x => x.name == sub.SceneName))
						{
							if (LoadedSubScenes == null) LoadedSubScenes = new List<Scene>();
							var scene                                    = allScenes.Find(x => x.name == sub.SceneName);
							LoadedSubScenes.AddIfNotExists(scene);
						}
					}
				}
			}

			/*/// <summary>
			/// A shorthand for requesting the loader to unload all scenes tracked by this GameScene.
			/// </summary>
			public void RequestUnload(bool unloadChildren)
			{

			}*/
		}

		/*public class LevelScene
		{
			/// <summary>
			/// Gets the level component of the scene.
			/// </summary>
			[CanBeNull] public Level Level { get; }

			public LevelScene(Level level, Scene scene)
			{
				Level = level;
				Scene = scene;
			}
		}*/

		/// <summary>
		/// An active operation being carried out by the loader.
		/// </summary>
		public class BaseOperation
		{
			public OpRequest Request;

			//Because field hiding messes with some things.
			protected virtual OpRequest _Request() => Request;

			protected BaseOperation() { }

			public BaseOperation(OpRequest _request)
			{
				StepIndex = 0;
				Request   = _request;
			}

			public BaseOperation(List<StepBase> _steps, OpRequest _request) : this(_request)
			{
				Steps = _steps;
			}

			public int            StepIndex;
			public List<StepBase> Steps;
			public bool           Done        => StepIndex >= Steps.Count;
			public StepBase       CurrentStep => Steps[StepIndex];

			public void Update()
			{
				while (!Done && CurrentStep.DoStep(this))
				{
					StepIndex++;
					_Request().UpdateProgress();
				}

				if (Done) _Request().OnOpDone();
			}
		}


		public struct AsyncOpPair
		{
			public AsyncOperation AsyncOp;
			public string         sceneName;

			public AsyncOpPair(AsyncOperation asyncOp, string sceneName)
			{
				AsyncOp        = asyncOp;
				this.sceneName = sceneName;
			}
		}


		/// <summary>
		/// Any operation that loads a main scene and a group of optional SubScenes into a new GameScene
		/// </summary>
		public class LoadOperation : BaseOperation
		{
			public new LoadRequest Request;
			public     bool        AllowSceneActivation { get; set; }

			protected override OpRequest _Request() => Request;

			public GameScene gameScene;

			public SceneReference SceneRef;
			public AsyncOperation AsyncOp;
			public Scene          GeneratedScene;

			//public List<AsyncOperation> SubAsyncOps;
			public List<AsyncOpPair>    SubAsyncOps;
			public List<SceneReference> SubSceneRefs;
			public List<Scene>          GeneratedSubScenes;

			public LoadOperation(LoadRequest _request)
			{
				Request            = _request;
				SubAsyncOps        = new List<AsyncOpPair>();
				SubSceneRefs       = new List<SceneReference>();
				GeneratedSubScenes = new List<Scene>();
			}

			public LoadOperation(List<StepBase> _steps, LoadRequest _request) : this(_request)
			{
				Steps = _steps;
			}
		}

		public class UnloadOperation : BaseOperation
		{
			public new         UnloadRequest Request;
			protected override OpRequest     _Request() => Request;

			public List<AsyncOpPair> SubAsyncOps;

			public UnloadOperation(UnloadRequest _request)
			{
				Request     = _request;
				SubAsyncOps = new List<AsyncOpPair>();
			}

			public UnloadOperation(List<StepBase> _steps, UnloadRequest _request) : this(_request)
			{
				Steps = _steps;
			}
		}
	}
}