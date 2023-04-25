using System;
using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Scripting;
using Anjin.Util;
using Anjin.Utils;
using Cinemachine;
using Combat.Data;
using Combat.Scripting;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Components.Cinemachine;
using Util.Odin.Attributes;

namespace Combat
{
	/// <summary>
	/// A controller for a camera to use in combat.
	///
	/// Base functionality
	/// ----------------------------------------
	/// Manages a follow and look point assigned to the VCam.
	/// Two corresponding transforms are created with a MotionComponent,
	/// and position of the VCam points is updated each frame to match motion points (in Update())
	/// In synchronized mode, both points will be set to the body motion.
	/// In separate mode, the points match each their corresponding motion points.
	///
	/// Scripting
	/// ----------------------------------------
	/// This script implements scripting functionalities and loads the arena-camera-states.lua script for its environment.
	/// We add a coplayer to the ArenaCamera in order to run animation functions in this environment.
	/// Refer to the States enum to see which states are available. (the enum values are functions in the script)
	/// If the script function doesn't exist, nothing happens and we continue with the existing animation.
	/// When the function completes, it is automatically replayed from the start.
	/// Each function receives an int argument "repetition" which is the number of times the function has been called.
	///
	/// </summary>
	public class ArenaCamera : MonoBehaviour, ICamController
	{
		public enum BodyLookModes
		{
			Synchronized,
			Separate,
		}

		/// <summary>
		/// The available script functions that can be implemented in arena-camera-states.lua
		/// </summary>
		public enum States
		{
			/// <summary>
			/// This state function should not be used.
			/// It is a special state to indicate that we have not yet started playing anything.
			/// (such as on startup)
			/// </summary>
			none,

			/// <summary>
			/// This state function should not be used.
			/// It is a special state to indicate that we are executing a custom function by string.
			/// </summary>
			custom,

			/// <summary>
			/// The default state of the camera.
			/// </summary>
			idle,

			/// <summary>
			/// The state when on the main screen of the triangle menu with categories.
			/// </summary>
			choose_category,

			/// <summary>
			/// The state when we are choosing a skill from the triangle menu.
			/// </summary>
			choose_action,

			/// <summary>
			/// The state when we are confirming hold with the target UI.
			/// </summary>
			confirm_hold,

			/// <summary>
			/// The state when we are choosing a target for a skill or sticker.
			/// </summary>
			choose_action_target,

			/// <summary>
			/// The state when we are choosing a target for a formation movement.
			/// </summary>
			choose_formation_target,
		}


		[SerializeField] public CinemachineVirtualCamera VCam;

		[FormerlySerializedAs("_orbitExtension")]
		public CinemachineOrbit Orbit;

		[SerializeField] public Transform OriginPoint;

		// State
		// ----------------------------------------
		[Title("Debug")]
		[DebugVars]
		[NonSerialized] public Transform lookPoint, bodyPoint;
		[NonSerialized] public MotionBehaviour lookMotion, bodyMotion;
		[NonSerialized] public Coplayer        coplayer;
		[NonSerialized] public Target          target;
		[NonSerialized] public int             elapsedRepeats;
		[NonSerialized] public int             maxRepeats;

		[NonSerialized] public CinemachineNoiseController noise;
		[NonSerialized] public BodyLookModes              bmmode;

		private Battle          _battle;
		private MotionBehaviour _lookMotion, _bodyMotion;
		private Vector3         _lookOffset = Vector3.zero;
		private States          _state;
		private Table           _stateEnv;

		private object[] _args = new object[1];

		public Coplayer.InitialCamState InitialState;

		//private CamController _controller;

		private void Awake()
		{
			InitialState = new Coplayer.InitialCamState(VCam);
		}

		private void Start()
		{
			MotionBehaviour CreateMotionPoint(string name)
			{
				var obj = new GameObject(name);
				obj.transform.parent = gameObject.transform;

				MotionBehaviour motion = obj.AddComponent<MotionBehaviour>();
				motion.AutoDestroy = false;
				motion.EnableStay  = true;

				if (OriginPoint != null)
				{
					obj.transform.position = OriginPoint.position;
				}

				return motion;
			}

			bodyMotion  = CreateMotionPoint("Follow Motion");
			_bodyMotion = bodyMotion;

			lookMotion  = CreateMotionPoint("Look Motion");
			_lookMotion = lookMotion;

			bodyPoint                  = new GameObject("Body Point").transform;
			bodyPoint.transform.parent = gameObject.transform;
			VCam.Follow                = bodyPoint;

			lookPoint                  = new GameObject("Look Point").transform;
			lookPoint.transform.parent = gameObject.transform;
			VCam.LookAt                = lookPoint;

			noise = VCam.GetOrAddComponent<CinemachineNoiseController>();

			coplayer               =  gameObject.GetOrAddComponent<Coplayer>();
			coplayer.afterComplete += coplayer_OnAfterComplete;
		}

		private void OnDisable()
		{
			LuaChangeWatcher.ClearWatches(this);
		}

		private void ResetState()
		{
			if (coplayer != null)
				coplayer.Stop();

			_state         = States.idle;
			elapsedRepeats = 0;
			maxRepeats     = -1;
		}

		public void SetBattle([CanBeNull] BattleRunner runner)
		{
			if (runner != null)
			{
				_battle = runner.battle;

				coplayer.baseState.battle = runner;
				coplayer.AddResource(VCam);

				GameCams.Push(this);
				ReloadScriptAndSetIdle();
			}
			else
			{
				_battle = null;

				coplayer.RemoveResource(VCam);

				ResetState();
				coplayer.baseState.battle = null;
				VCam.Priority             = -1;
				GameCams.Pop(this);
				LuaChangeWatcher.ClearWatches(this);
			}
		}

		#region Scripting

		private void coplayer_OnAfterComplete()
		{
			// Either play the next state returned by the anim function, or repeat it
			DynValue ret = coplayer.coroutine?.returnValue;
			if (ret?.Type == DataType.String)
			{
				Play(_stateEnv, ret.String);
			}
			else
			{
				elapsedRepeats++;
				if (maxRepeats != -1 && elapsedRepeats >= maxRepeats)
					return;

				coplayer.Replay(LuaUtil.Args(elapsedRepeats));
			}
		}

		public void ReloadScriptAndSetIdle()
		{
			States prevstate = _state;

			ResetState();

			LuaChangeWatcher.BeginCollecting();
			_stateEnv = Lua.NewScript("arena-camera-states");
			Lua.LoadFileInto("anim-api", _stateEnv);
			Lua.LoadFileInto("std-anim", _stateEnv);
			LuaChangeWatcher.EndCollecting(this, ReloadScriptAndSetIdle);

			PlayState(!prevstate.IsAnimationState()
				? States.idle
				: prevstate);
		}


		private void RefreshScriptRefs()
		{
			_stateEnv[LuaEnv.OBJ_BATTLE] = _battle;
			_stateEnv[LuaEnv.OBJ_ARENA]  = _battle.arena;
			_stateEnv[LuaEnv.OBJ_USER]   = _battle?.ActiveAction.acter;
			_stateEnv[LuaEnv.OBJ_TARGET] = target;

			_args[0] = elapsedRepeats;
		}

		#endregion

		#region Play Functions

		[NotNull]
		public Coplayer Play([NotNull] Table env, [NotNull] string func, int maxRepeats = -1)
		{
			if (_battle.runner.logVisuals)
				_battle.runner.LogTrace("--", $"Play {func} ({env.GetEnvName()})");

			if (!env.ContainsKey(func))
				return coplayer;

			if (env.ContainsKey("test"))
				func = "test";


			ResetState();
			RefreshScriptRefs();
			this.maxRepeats = maxRepeats;

			coplayer.Play(env, func, _args).Forget();
			return coplayer;
		}

		[NotNull]
		public Coplayer Play([NotNull] Table env, [NotNull] Closure func, int maxRepeats = -1)
		{
			this.LogTrace("--", $"Play {func} ({env.GetEnvName()}).");

			ResetState();
			RefreshScriptRefs();
			this.maxRepeats = maxRepeats;

			coplayer.Play(env, func, _args).Forget();
			return coplayer;
		}


		/// <summary>
		/// Play a state of the camera. States are found in
		/// </summary>
		/// <param name="newstate"></param>
		/// <param name="force"></param>
		public void PlayState(States newstate, bool force = false)
		{
			if (_state == newstate && !force)
				return;

			ResetState();

			Play(_stateEnv, Enum.GetName(typeof(States), newstate) ?? throw new InvalidOperationException());
			_state = newstate;
		}

		#endregion

		public void SetZero()
		{
			bodyMotion.Stop();
			lookMotion.Stop();

			bodyPoint.transform.position = _battle.arena.transform.position;
			lookPoint.transform.position = _battle.arena.transform.position;

			Orbit.Coordinates = new SphereCoordinate();

			VCam.UpdateCameraState(Vector3.up, 120f);
		}

		public void SetFlip(bool enable)
		{
			Orbit.invertAzimuth = enable;
		}

		public void SetMode(BodyLookModes mode, out MotionBehaviour body, out MotionBehaviour look)
		{
			if (bmmode == BodyLookModes.Synchronized)
			{
				// This is so that going from synchronized to separate doesn't cause a jerk motion
				lookMotion.transform.position = bodyMotion.transform.position;
			}

			if (mode == BodyLookModes.Synchronized)
			{
				_bodyMotion = body = bodyMotion;
				_lookMotion = look = bodyMotion;
			}
			else
			{
				_bodyMotion = body = bodyMotion;
				_lookMotion = look = lookMotion;
			}

			bmmode = mode;
			_bodyMotion.Reset();
			_lookMotion.Reset();
		}

		private void Update()
		{
			if (_battle != null)
			{
				bodyPoint.position = _bodyMotion.transform.position;
				lookPoint.position = _lookMotion.transform.position + _lookOffset;
			}
		}


		#region GameCams

		public void OnActivate() { }

		public void OnRelease(ref CinemachineBlendDefinition? blend)
		{
			VCam.Priority = GameCams.PRIORITY_INACTIVE;
			blend         = GameCams.Cut;
		}

		public void ActiveUpdate()
		{
			VCam.Priority = 100; //GameCams.PRIORITY_ACTIVE;
		}

		public void GetBlends(ref CinemachineBlendDefinition? blend, ref CinemachineBlenderSettings settings) { blend = GameCams.Cut; }

		#endregion
	}

	public static class ArenaCameraStatesExtensions
	{
		public static bool IsAnimationState(this ArenaCamera.States prevstate) => !(prevstate == ArenaCamera.States.none || prevstate == ArenaCamera.States.custom);
	}
}