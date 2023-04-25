using System;
using System.Collections.Generic;
using System.Diagnostics;
using Anjin.Actors;
using Anjin.Audio;
using Anjin.Cameras;
using Anjin.Nanokin;
using Anjin.Scripting;
using Anjin.Scripting.Waitables;
using Anjin.UI;
using Anjin.Util;
using Anjin.Utils;
using Cinemachine;
using Combat;
using Combat.Data;
using Combat.Startup;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Controllers;
using Overworld.Cutscenes.Timeline;
using Overworld.Tags;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using Util;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;
using Debug = UnityEngine.Debug;
using g = ImGuiNET.ImGui;
using Trigger = Anjin.Nanokin.Map.Trigger;

namespace Overworld.Cutscenes
{
	/// <summary>
	/// A player which offers stateful interactions and features for a coroutine function.
	/// For most uses, simply use implicitly with Lua.InvokePlayer
	/// </summary>
	[DefaultExecutionOrder(1)]
	public partial class Coplayer : SerializedMonoBehaviour, ICamController, IRecyclable, IActivable, IDialogueTextboxInvoker
	{
		// NOTE: Do not reorder
		public enum Steps { Cleared, Ready, PrePlay, Paused, Playing, Skipping }

		public enum SkipMode { Instant } // NOTE: leaving the door open for adding other things like fast forwarding ect

		public bool Logging;

		//[DebugVars]
		[NonSerialized, ShowInPlay] public Steps    step;
		[NonSerialized, ShowInPlay] public SkipMode skipMode;

		[NonSerialized, ShowInPlay] public Table                       script;
		[NonSerialized, ShowInPlay] public CutsceneBrain               actorBrain;
		[NonSerialized, ShowInPlay] public State                       baseState;
		[NonSerialized, ShowInPlay] public State                       state;
		[NonSerialized, ShowInPlay] public CoroutineInstance           coroutine;
		[NonSerialized, ShowInPlay] public List<CoroutineInstance>     subroutines;
		[NonSerialized, ShowInPlay] public List<Coplayer>              subplayers = new List<Coplayer>();
		[NonSerialized, ShowInPlay] public List<DirectedBase>          members;
		[NonSerialized, ShowInPlay] public List<string>                memberTags;
		[NonSerialized, ShowInPlay] public CinemachineBlendDefinition  vcamBlend     = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.Cut, 0);
		[NonSerialized, ShowInPlay] public CinemachineBlendDefinition? outgoingBlend = null;

		[NonSerialized, ShowInPlay] public GameObject   sourceObject;
		[NonSerialized, ShowInPlay] public Interactable sourceInteractable;
		[NonSerialized, ShowInPlay] public Trigger      sourceTrigger;

		public CharacterInteractCamera interactCamera;

		/// <summary>
		/// Allows specifying anim flags to the coroutine, which can be accessed to modify the code.
		/// </summary>
		[NonSerialized, ShowInPlay] public List<string> animflags = new List<string>();

		/// <summary>
		/// Allows specifying skip flags to the coroutine, which can be accessed to skip parts of the code.
		/// </summary>
		[NonSerialized, ShowInPlay] public List<string> skipflags = new List<string>();

		/// <summary>
		/// Auto return to the Lua coplayer pool on finish.
		/// </summary>
		[NonSerialized, ShowInPlay] public bool autoReturn;

		[NonSerialized, ShowInPlay] public bool isActivated;


		[ShowInPlay] private List<CoroutineManaged> _manageds = new List<CoroutineManaged>();

		private TimeScalable _timescale;

		/// <summary>
		/// Invoked when the coplayer is stopped, externally or from completion.
		/// </summary>
		[NonSerialized] public Action afterStopped;

		/// <summary>
		/// Invoked before completion of this execution.
		/// </summary>
		[NonSerialized] public Action beforeComplete;

		/// <summary>
		/// Invoked after completion of this execution.
		/// </summary>
		[NonSerialized] public Action afterComplete;

		/// <summary>
		/// afterStopped but gets cleared after.
		/// </summary>
		[NonSerialized] public Action afterStoppedTmp;

		/// <summary>
		/// beforeComplete but gets cleared after.
		/// </summary>
		[NonSerialized] public Action beforeCompleteTmp;

		/// <summary>
		/// afterComplete but gets cleared after.
		/// </summary>
		[NonSerialized] public Action afterCompleteTmp;

		/// <summary>
		/// Invoked when the coplayer is stopped, externally or from completion.
		/// </summary>
		[NonSerialized] public Action<bool> onPauseUpdated;

		// Resources
		// ----------------------------------------
		private Dictionary<string, PlayableDirector>                  _directors;
		private Dictionary<string, CinemachineVirtualCamera>          _cameras;
		private Dictionary<string, CutsceneObject>                    _objects;
		private List<CoplayerTimelineSystem>                          _timelineSystems;
		private Dictionary<CinemachineVirtualCamera, InitialCamState> _restoreCamStates;

		private int _skipCooldown;

		public bool Ended => coroutine != null && coroutine.Ended && subplayers.Count == 0;

		public bool Running => coroutine != null && coroutine.Running;

		public bool IsPlaying => step >= Steps.Playing;
		public bool Skipping  => step == Steps.Skipping;

		private static List<CoroutineManaged> _scratchManageds = new List<CoroutineManaged>();

		[ShowInPlay]
		public static List<Coplayer> All = new List<Coplayer>();

		// TODO this might not actually be needed, maybe merge State down into coplayer.
		public struct State
		{
			// public GameObject   selfObject;
			// public Fighter      selfFighter;
			// public ActorBase    selfActor;
			public TimeScalable timescale;

			public Table            graph;
			public AudioZone        musicZone;
			public PlayableDirector director;
			public int              choiceResult;
			public BattleOutcome    combatOutcome;
			public string           vcamName;
			public VCamTarget       vcamTarget;

			public bool TextboxSoftAuto;

			public string mainCameraName;
			public bool   noOutgoingBlend;

			// public CinemachineBlendDefinition mainBlend; // TODO
			// public float                      mainDuration; // TODO

			public BattleRunner battle;
			public ProcTable    procs;

			// public void SetFighterSelf(Fighter value)
			// {
			// 	selfFighter = value;
			// 	selfActor   = value?.actor;
			// 	selfObject  = value?.actor.gameObject;
			// 	timescale   = value.actor.GetComponent<TimeScalable>() ?? timescale;
			// }
		}

		private void Awake()
		{
			members           = new List<DirectedBase>();
			memberTags        = new List<string>();
			subroutines       = new List<CoroutineInstance>();
			_timelineSystems  = new List<CoplayerTimelineSystem>();
			_directors        = new Dictionary<string, PlayableDirector>();
			_cameras          = new Dictionary<string, CinemachineVirtualCamera>();
			_objects          = new Dictionary<string, CutsceneObject>();
			_restoreCamStates = new Dictionary<CinemachineVirtualCamera, InitialCamState>();
			_timescale        = gameObject.GetOrAddComponent<TimeScalable>();

			// Get a brain for this player
			actorBrain = gameObject.GetOrAddComponent<CutsceneBrain>();

			Enabler.Register(gameObject);

			_skipCooldown = 0;
		}

		private void OnEnable()  => All.AddIfNotExists(this);
		private void OnDisable() => All.Remove(this);
		private void OnDestroy() => All.Remove(this);

		public Coplayer AutoReturn()
		{
			if (!autoReturn)
			{
				autoReturn      =  true;
				afterStoppedTmp += () => Lua.ReturnPlayer(this);
			}

			return this;
		}

		// Basic Operations
		// ----------------------------------------

		public void Setup(Table script) => this.script = script;

		[DebuggerHidden]
		public UniTask Play([NotNull] Table script, [NotNull] Closure function, [CanBeNull] object[] args = null)
		{
			CoroutineInstance co = Lua.CreateCoroutine(script, function, args);
			if (co == null)
				return UniTask.CompletedTask;

			Prepare(script, co);
			return Play();
		}

		[DebuggerHidden]
		public UniTask Play([NotNull] Table script, [NotNull] string function, [CanBeNull] object[] args = null)
		{
			CoroutineInstance co = Lua.CreateCoroutine(script, function, args);
			if (co == null)
				return UniTask.CompletedTask;

			Prepare(script, co);
			return Play();
		}

		/// <summary>
		/// Replay the same function as before, possibly with different arguments.
		/// </summary>
		public UniTask Replay([CanBeNull] object[] newargs = null) => Play(script, coroutine.closure, newargs);

		/// <summary>
		/// Play a coroutine instance.
		/// </summary>
		public UniTask Play(Table script, [NotNull] CoroutineInstance coroutine)
		{
			Prepare(script, coroutine);
			return Play();
		}

		public void Prepare([NotNull] Table script, [NotNull] CoroutineInstance coroutine)
		{
			step = Steps.PrePlay;

			if (Logging) this.LogTrace("--", $"Setup :: {script.GetEnvName()} ...");

			this.script    = script;
			this.coroutine = coroutine;

			state           = baseState;
			state.timescale = _timescale;

			coroutine.onBeforeResume += () =>
			{
				this.script["coplayer"] = this;
			};
		}

		public async UniTask Play()
		{
			if (step < Steps.PrePlay)
			{
				this.LogWarn("Cannot play without preparing the coplayer first.");
				return;
			}
			else if (step > Steps.PrePlay)
			{
				this.LogWarn("Cannot play while the coplayer is already busy.");
				return;
			}

			// This fixes a VERY sneaky bug!
			// If we destroy an actor right before calling this function (on the same frame)
			// OnDestroy does not get called until the end of the frame.
			// Actors are deregistered from the ActorRegistry from Actor.OnDestroy, meaning that we can end up
			// retrieving an actor that gets destroyed the next frame!
			await UniTask.WaitForEndOfFrame();

			if (members.Count > 0)
			{
				if (Logging) this.Log($"[TRACE] (COPLAYER) using declared members: {members.JoinString()}");

				foreach (DirectedBase cutActor in members)
				{
					await cutActor.Load();
					cutActor.OnStart(this);
				}
			}

			if (state.mainCameraName != null)
				ControlCamera(true);

			step = Steps.Playing;

			foreach (KeyValuePair<string, CutsceneObject> pair in _objects)
			{
				pair.Value.gameObject.SetActive(true);
			}

			Continue(); // Initial execution (can't remember why this was needed...)
		}

		private void Continue()
		{
			coroutine.TryContinue(Time.deltaTime, step == Steps.Skipping);

			for (int i = 0; i < subroutines.Count; i++)
			{
				if (!subroutines[i].Ended)
				{
					subroutines[i].TryContinue(Time.deltaTime, step == Steps.Skipping);
				}

				if (subroutines[i].Ended)
				{
					subroutines.Remove(subroutines[i]);
					i--;
				}
			}

			if (coroutine.Ended)
			{
				beforeComplete?.Invoke();
				beforeCompleteTmp?.Invoke();
				beforeCompleteTmp = null;

				OnStop();

				afterComplete?.Invoke();
				afterCompleteTmp?.Invoke();
				afterCompleteTmp = null;
			}
		}

		public void StartSkipping()
		{
			if (step != Steps.Playing) return;
			step = Steps.Skipping;

			coroutine.OnBeginSkip();
		}

		public void StopSkipping()
		{
			if (step != Steps.Skipping) return;
			step          = Steps.Playing;
			_skipCooldown = 2;
		}


		/// <summary>
		/// Pause/resume the current execution.
		/// </summary>
		/// <param name="state"></param>
		public void Pause()
		{
			if (step == Steps.Paused) return;
			step = Steps.Paused;
			onPauseUpdated?.Invoke(true);
		}

		public void Unpause()
		{
			if (step != Steps.Paused) return;
			step = Steps.Playing;
			onPauseUpdated?.Invoke(false);
		}

		private void Update()
		{
			if (!IsPlaying) return;

			if (_skipCooldown > 0) {
				_skipCooldown--;
				return;
			}

			Continue();

			for (var i = 0; i < members.Count; i++)
			{
				members[i].Update();
			}

			for (var i = 0; i < _manageds.Count; i++)
			{
				CoroutineManaged man = _manageds[i];

				// We do this check twice to make sure we remove it at the earliest possible frame
				// This might be overkill but better safe than sorry, people have lost their minds
				// over 1 frame visual defects in the past
				if (!man.Active)
				{
					EndAndRemoveManagedAt(man, i--);
					continue;
				}

				man.OnCoplayerUpdate(state.timescale.current);

				if (!man.Active)
				{
					EndAndRemoveManagedAt(man, i--);
				}
			}

			for (var i = 0; i < subplayers.Count; i++)
			{
				if (subplayers[i].Ended)
				{
					subplayers.RemoveAt(i--);
				}
			}
		}


		/// <summary>
		/// Stop the current coroutine mid-execution.
		/// </summary>
		public void Stop()
		{
			if (step <= Steps.Ready) return;

			coroutine?.Cancel();
			foreach (CoroutineInstance subroutine in subroutines)
			{
				subroutine?.Cancel();
			}

			OnStop();
		}

		/// <summary>
		/// Recycle the coplayer so it can be reused by
		/// something else.
		/// </summary>
		public void Recycle()
		{
			RestoreReady();
			RestoreCleared();
		}

		/// <summary>
		/// Completely clear everything on this coplayer to a clean slate.
		/// </summary>
		public void RestoreCleared()
		{
			step = Steps.Cleared;

			state = new State
			{
				choiceResult = -1
			};

			isActivated = true; // BUG shouldn't this be false?

			foreach (DirectedBase member in members)
			{
				member.Release();
			}

			coroutine = null;
			subroutines.Clear();
			members.Clear();
			_manageds.Clear();
			script       = null;
			autoReturn   = false;
			sourceObject = null;

			baseState = new State();

			_directors.Clear();
			_cameras.Clear();
			_objects.Clear();
			_restoreCamStates.Clear();

			// Clean up tags
			memberTags.Clear();
		}

		/// <summary>
		/// Restore the state to pre-execution state (sleeping)
		/// - Restore baseState
		/// - Restore camera position/rotation
		/// </summary>
		public void RestoreReady()
		{
			step = Steps.Ready;

			animflags.Clear();
			skipflags.Clear();

			isActivated = true; // BUG shouldn't this be false?

			// Cleanup the members
			foreach (DirectedBase actor in members)
			{
				actor.OnStop(this);
			}

			// Cleanup the brain
			actorBrain.actors.Clear();

			// This function could be called from anywhere, even on game start before the GameCams are loaded
			if (GameCams.Live != null)
			{
				if (GameCams.Live._controller == this)
					GameCams.ReleaseController();
			}

			// Restore vcams to their initial state if possible
			foreach ((string _, CinemachineVirtualCamera value) in _cameras)
			{
				if (!value.HasComponent<VCamNoCoplayerReset>() && _restoreCamStates.TryGetValue(value, out InitialCamState camState))
					camState.ResetUsing(value);
			}

			// Stop manageds.
			foreach (CoroutineManaged managed in _manageds)
			{
				if (Logging) this.Log($"[TRACE] Stop managed {managed}");
				if (!managed.manual)
					managed.Stop();
			}

			_manageds.Clear();

			foreach (KeyValuePair<string, CutsceneObject> pair in _objects)
			{
				if (pair.Value.OnlyActiveDuringCutscene)
					pair.Value.gameObject.SetActive(false);
			}

			foreach (CoplayerTimelineSystem system in _timelineSystems)
			{
				system.ResetDirector();
			}

			// Clean up any music we used
			ClearMusic();

			if (interactCamera != null)
			{
				GameCams.ReturnCharacterInteraction(interactCamera);
				interactCamera = null;
			}

			// Reset the state
			state = new State();
		}


		// Subroutines
		// ----------------------------------------
		public WaitableCoroutineInstance NewSubroutine(Closure func, object[] args = null)
		{
			if (func == null) return null;
			var instance = new CoroutineInstance(script, func, Lua.envScript.CreateCoroutine(func).Coroutine, args);
			subroutines.Add(instance);

			return new WaitableCoroutineInstance(instance);
		}


		// Managed Management
		// ----------------------------------------

		/// <summary>
		/// Add a CoroutineManaged to this player.
		/// </summary>
		/// <param name="managed"></param>
		public TManaged RegisterManaged<TManaged>(TManaged managed)
			where TManaged : CoroutineManaged
		{
			if (!Running)
			{
				if (Logging) this.LogError("CoroutinePlayer: Cannot add managed {managed} to CoroutinePlayer because it's not playing!");
				return managed;
			}

			if (managed == null)
			{
				this.LogError("Tried to add null managed.");
				return null;
			}

			_manageds.Add(managed);
			_scratchManageds.Add(managed);

			managed.coplayer = this;
			managed.Start();

			if (Logging) this.Log($"[TRACE] Coplayer.Manage {managed}");

			return managed;
		}

		/// <summary>
		/// Add a CoroutineManaged to this player.
		/// </summary>
		/// <param name="managed"></param>
		public void RegisterManaged(int id, CoroutineManaged managed)
		{
			if (!Running)
			{
				if (Logging) this.LogError("CoroutinePlayer: Cannot add managed {managed} to CoroutinePlayer because it's not playing!");
				return;
			}

			if (managed == null)
			{
				this.LogError("Tried to add null managed.");
				return;
			}

			// This is a development safeguard more than anything
			for (var i = 0; i < _manageds.Count; i++)
			{
				CoroutineManaged existing = _manageds[i];
				if (existing.id == id)
				{
					if (Logging) this.LogError($"Replacing an existing ({existing}) with a new one ({managed}) because they have matching IDs.");
					_manageds.RemoveAt(i--);
					break;
				}
			}

			_manageds.Add(managed);
			_scratchManageds.Add(managed);

			managed.id       = id;
			managed.coplayer = this;
			managed.Start();

			if (Logging) this.Log($"[TRACE] Coplayer.Manage {managed}");
		}


		/// <summary>
		/// Add a CoroutineManaged to this player,
		/// or replace an existing one by ID.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="managed"></param>
		public bool GetManaged<TManaged>(int id, out TManaged ret)
			where TManaged : CoroutineManaged
		{
			if (step <= Steps.Ready)
			{
				if (Logging) this.LogError("CoroutinePlayer: Cannot get managed because the CoroutinePlayer isn't playing!");
				ret = null;
				return false;
			}

			// A dictionary might be better here if we start making extended use of this
			for (var i = 0; i < _manageds.Count; i++)
			{
				CoroutineManaged existing = _manageds[i];
				if (existing.id == id)
				{
					ret = (TManaged)existing; // Should always be a valid cast if everything goes right
					return true;
				}
			}

			ret = null;
			return false;
		}

		/// <summary>
		/// Remove a CoroutineManaged by ID.
		/// </summary>
		/// <param name="id"></param>
		public void StopAndRemoveManaged(string id)
		{
			StopAndRemoveManaged(id.GetHashCode());
		}

		/// <summary>
		/// Remove a CoroutineManaged by ID.
		/// </summary>
		/// <param name="id"></param>
		public void StopAndRemoveManaged(int id)
		{
			for (var i = 0; i < _manageds.Count; i++)
			{
				CoroutineManaged managed = _manageds[i];
				if (managed.id == id)
				{
					StopAndRemoveManagedAt(managed, i);
					return;
				}
			}
		}

		private void StopAndRemoveManaged((GameObject go, string) id)
		{
			StopAndRemoveManaged(id.GetHashCode());
		}

		/// <summary>
		/// Try to restart a manage by ID.
		/// </summary>
		/// <returns>Whether or not we successfuly restarted a managed with the matching ID.</returns>
		public bool RestartManaged(int id)
		{
			if (GetManaged(id, out CoroutineManaged mtrail))
			{
				mtrail.Stop();
				mtrail.Start();

				return true;
			}

			return false;
		}

		private TManaged ReplaceManaged<TManaged>(string id, TManaged managed)
			where TManaged : CoroutineManaged
			=> ReplaceManaged(id.GetHashCode(), managed);

		private void ReplaceManaged<T1, T2>((T1, T2) tuple, CoroutineManaged managed)
			=> ReplaceManaged(tuple.GetHashCode(), managed);

		private TManaged ReplaceManaged<TManaged>(int id, TManaged managed)
			where TManaged : CoroutineManaged
		{
			if (GetManaged(id, out CoroutineManaged mcurrent))
			{
				mcurrent.Stop();
				_manageds.Remove(mcurrent);
			}

			StopAndRemoveManaged(id);
			RegisterManaged(id, managed);
			return managed;
		}


		private void StopAndRemoveManagedAt(CoroutineManaged managed, int i)
		{
			if (Logging) this.LogTrace("xx", $"unmanage inactive :: {managed}");

			managed.coplayer = null;
			managed.Stop();
			_manageds.RemoveAt(i);
		}

		private void EndAndRemoveManagedAt(CoroutineManaged managed, int i)
		{
			if (Logging) this.LogTrace("xx", $"unmanage inactive :: {managed}");

			managed.coplayer = null;
			managed.End();
			_manageds.RemoveAt(i);
		}

		/// <summary>
		/// Release the music played by this player.
		/// </summary>
		private void ClearMusic()
		{
			if (state.musicZone != null)
			{
				AudioManager.RemoveZone(state.musicZone);
				state.musicZone = null;
			}
		}

		public void ControlCamera(bool state)
		{
			/*if (Running)
			{*/
			if (state)
				GameCams.SetController(this);
			else if (GameCams.Live._controller == this)
				GameCams.ReleaseController();
			//}
		}

		public void StopWith(Closure func)
		{
			if (!Running) return;

			if (Logging) this.LogWarn("Cannot StopWith while not playing. Is this intentional?", nameof(StopWith));
			func?.Call();
			Stop();
		}

		// Resources
		// ----------------------------------------

		public void AddResource(string name, CinemachineVirtualCamera cam)
		{
			_cameras[name] = cam;
			if (!_restoreCamStates.ContainsKey(cam))
				_restoreCamStates[cam] = new InitialCamState(cam);
		}

		public void AddResource(CinemachineVirtualCamera cam)
		{
			_cameras[cam.name] = cam;
			if (!_restoreCamStates.ContainsKey(cam))
				_restoreCamStates[cam] = new InitialCamState(cam);
		}

		public void RemoveResource(CinemachineVirtualCamera cam)
		{
			if (_restoreCamStates.ContainsKey(cam))
				_restoreCamStates.Remove(cam);
		}

		/// <summary>
		/// Discover resources to use under a root object.
		/// </summary>
		/// <param name="root"></param>
		public void DiscoverResources()
		{
			// Cameras
			// ----------------------------------------
			CinemachineVirtualCamera[] children = sourceObject.GetComponentsInChildren<CinemachineVirtualCamera>(true);
			foreach (CinemachineVirtualCamera childCam in children)
			{
				_cameras[childCam.name]     = childCam;
				_restoreCamStates[childCam] = new InitialCamState(childCam);
				childCam.Priority           = -1;
			}

			// Timelines
			// ----------------------------------------

			CoplayerTimelineSystem[] systems = sourceObject.GetComponentsInChildren<CoplayerTimelineSystem>(true);
			foreach (CoplayerTimelineSystem system in systems)
			{
				system.Owner = this;
				_timelineSystems.Add(system);
			}

			PlayableDirector[] directors = sourceObject.GetComponentsInChildren<PlayableDirector>(true);
			foreach (PlayableDirector director in directors)
			{
				_directors[director.name] = director;

				director.gameObject.GetOrAddComponent<TimeMarkerSystem>().Reset();

				CoplayerTimelineSystem system = director.gameObject.GetOrAddComponent<CoplayerTimelineSystem>();
				system.Owner = this;
				_timelineSystems.Add(system);

				director.InjectCinemachineBrain();
				director.Stop();
			}

			CutsceneObject[] objects = sourceObject.GetComponentsInChildren<CutsceneObject>(true);
			foreach (CutsceneObject obj in objects)
			{
				_objects[obj.name] = obj;
				if (obj.OnlyActiveDuringCutscene)
					obj.gameObject.SetActive(false);
			}
		}

		// Use Declarations
		// ----------------------------------------

		public DirectedBase UseMember(DirectedBase member)
		{
			if (members.Contains(member)) return member;
			if (member == null) return null;

			members.Add(member);
			member.Load();
			if (Running)
				member.OnStart(this);

			if (member.gameObject && member is DirectedActor actor && actor.guest)
			{
				member.gameObject.SetActive(isActivated);
			}

			return member;
		}

		public DirectedActor UseMember(Actor value, Table options)
		{
			var actor = new DirectedActor(value, options);
			UseMember(actor);
			return actor;
		}


		// State Features
		// ----------------------------------------

		private void ControlGame(bool state)
		{
			if (state)
			{
				Cutscene cut = null;

				script.TryGet("this", out cut, cut);

				GameController.Live.BeginCutscene(cut);
			}
			else
			{
				GameController.Live.EndCutscene();
			}
		}


		private void OnStop()
		{
			if (Logging) this.LogTrace("--", $"Stop :: {script.GetEnvName()}");
			RestoreReady();

			if (sourceInteractable != null) sourceInteractable.locks--;
			if (sourceTrigger != null) sourceTrigger.locks--;

			afterStopped?.Invoke();
			afterStoppedTmp?.Invoke();
			afterStoppedTmp = null;
		}

		/// <summary>
		/// Return the single waitable in _scratchWaitables, or a WaitableGroup if there are more than one.
		/// </summary>
		private static object CollectNewManageds()
		{
			if (_scratchManageds.Count == 1)
			{
				CoroutineManaged ret1 = _scratchManageds[0];
				_scratchManageds.Clear();

				return ret1;
			}

			// Can't re-use the same list here in this case, since we may
			// wanna use
			CoroutineManaged[] retn = _scratchManageds.ToArray();
			_scratchManageds.Clear();
			return retn;
		}

		public void OnTimelineProxyNotification(CoplayerTimelineSystem system, Playable origin, INotification notification, object context)
		{
			switch (notification)
			{
				case LuaCallMarker marker:
					if (Logging) this.Log($"Coplayer recieved LuaCallMarker, function name {marker.FunctionName}");

					CoroutineInstance coroutine = Lua.CreateCoroutine(script, marker.FunctionName);

					if (coroutine != null)
					{
						subroutines.Add(coroutine);

						coroutine.skipEverything = step == Steps.Skipping;
						if (marker.PauseTillDone)
						{
							system.PauseOnMarker(marker, new WaitableCoroutineInstance(coroutine));
						}
					}

					//_script.TryCall(Lua.envScript, marker.FunctionName);
					break;
			}
		}

		public void DeactivateCams()
		{
			foreach (CinemachineVirtualCamera cam in _cameras.Values)
			{
				cam.Priority = GameCams.PRIORITY_INACTIVE;
			}
		}

		public void OnRelease(ref CinemachineBlendDefinition? blend)
		{
			DeactivateCams();
			if (outgoingBlend != null)
			{
				blend         = outgoingBlend.Value;
				outgoingBlend = null;
			}
			else if (!state.noOutgoingBlend)
			{
				blend = GameCams.Cut;
			}
			//outgoingBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 3);
		}

		public void ActiveUpdate()
		{
			DeactivateCams();

			string name = state.mainCameraName;

			if (name == null) name = state.vcamName;
			if (name == null) return;

			if (_cameras.TryGetValue(name, out CinemachineVirtualCamera childCam))
			{
				childCam.Priority = GameCams.PRIORITY_ACTIVE;
				return;
			}

			foreach (CameraShot shot in CameraShot.all)
			{
				if (shot.gameObject.name == name)
				{
					shot.vcam.Priority = GameCams.PRIORITY_ACTIVE;
					return;
				}
			}
		}

		public void GetBlends(ref CinemachineBlendDefinition? blend, ref CinemachineBlenderSettings settings)
		{
			blend = vcamBlend;
		}

		private void EnsureControl(DirectedActor actor)
		{
			actor.Control();
		}

		public struct InitialCamState
		{
			public Vector3    position;
			public Quaternion rotation;
			public float      fov;

			public InitialCamState(CinemachineVirtualCamera cam) : this()
			{
				position = cam.transform.position;
				rotation = cam.transform.rotation;
				fov      = cam.m_Lens.FieldOfView;
			}

			public void ResetUsing(CinemachineVirtualCamera cam)
			{
				cam.transform.position = position;
				cam.transform.rotation = rotation;
				cam.m_Lens.FieldOfView = fov;
			}
		}

		public void OnActivate()
		{
			if (Logging) this.Log($"COPLAYER: {name}, activate");

			isActivated = true;

			for (var i = 0; i < members.Count; i++)
			{
				DirectedBase member = members[i];
				if (member.gameObject)
				{
					Tag tag = member.gameObject.GetComponent<Tag>();

					if (member is DirectedActor actor && actor.guest && (!tag || tag.state)) // TODO we might wanna optimize the null-check for member.gameObject
						member.gameObject.SetActive(true);
				}
			}
		}

		public void OnDeactivate()
		{
			if (Logging) this.Log($"COPLAYER: {name}, deactivate");

			isActivated = false;

			for (var i = 0; i < members.Count; i++)
			{
				DirectedBase member = members[i];
				if (member.gameObject && member is DirectedActor actor && actor.guest) // TODO we might wanna optimize the null-check for member.gameObject
					member.gameObject.SetActive(false);
			}
		}

		public void OnImgui() { }


		// Textbox Control
		// ----------------------------------------

		public void SoftAutoActivated()   => state.TextboxSoftAuto = true;
		public void SoftAutoDeactivated() => state.TextboxSoftAuto = false;
		public bool SoftAuto              => state.TextboxSoftAuto;
	}
}