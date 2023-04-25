using System;
using System.Collections.Generic;
using Anjin.MP;
using Anjin.Nanokin;
using Anjin.Regions;
using Anjin.Scripting;
using Anjin.Scripting.Waitables;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	[LuaUserdata]
	public class PathFollowingBrain : ActorBrain, ICharacterActorBrain
	{
		public override int Priority => 5;

		//public PathingSettings settings = PathingSettings.Default;
		//public float WaitTimer = 0;

		public struct StartConfig {
			public string region_path;
			public bool   loop;
			public float  speed;
			public bool   teleport_on_begin_control;
		}

		public StartConfig? Config;

		public float?   Speed		= null;
		public LookMode LookMode	= LookMode.Forward;

		[NonSerialized, ShowInPlay]
		public int _target_index = 0;

		[ShowInPlay] private bool            _active;
		[ShowInPlay] private bool            _calculating;
		[ShowInPlay] private MPPath          _path;
		[ShowInPlay] private RegionPath      _rpath;
		[ShowInPlay] private PathFollowState _state;
		[ShowInPlay] private Vector3         _lookDir;
		[ShowInPlay] private WorldPoint      _lookPoint;

		/*[NonSerialized, ShowInPlay]
		public Dictionary<Actor, PathingState> states;*/

		void Awake()
		{
			_active      = false;
			_calculating = false;
			_state       = PathFollowState.Default;

			//WaitTimer 	= 0;
		}

		void Update()
		{
			/*if (WaitTimer > 0)
				WaitTimer -= Time.deltaTime;*/
		}

		public override async void OnBeginControl()
		{
			await GameController.TillIntialized();
			//await Lua.initTask;

			if (Config.HasValue) {
				StartConfig conf = Config.Value;
				await FollowPath(conf.region_path);

				if (conf.teleport_on_begin_control && _rpath != null && _rpath.Points.Count > 0) {
					actor.Teleport(_rpath.GetWorldPoint(0));
				}
			}

			/*if(states == null)
				states = new Dictionary<Actor, PathingState>();

			states[actor] = PathingState.Default;
			var state = states[actor];
			state.Start(settings);
			states[actor] = state;*/
		}

		public override void OnEndControl()
		{
			//states.Remove(actor);
			if (actor is ActorKCC kcc) {
				kcc.ClearVelocity();
				kcc.inputs.NoMovement();
			}
		}

		public void PollInputs(Actor character, ref CharacterInputs inputs)
		{
			if (!_active) {
				inputs.NoMovement();
				return;
			}


			{
				//If we have no paths at all, and we aren't calculating, we need to end pathing to avoid errors.
				if (!_calculating && _path == null) {
					EndPathing();
					goto end;
				}

				if (_calculating) return;

				MotionPlanning.DrawMPPathInEditorALINE(_path, Color.red);

				PathFollowState pfs      = _state;
				Vector3         position = actor.transform.position;

				NPCActor    npc = actor as NPCActor;
				PlayerActor plr = actor as PlayerActor;

				float spd = character.velocity.magnitude;

				if (npc) spd      = npc.CSettings.WalkSpeed;
				else if (plr) spd = plr.Settings.WalkRun.MaxSpeed;

				if (Speed.HasValue)
					spd = Speed.Value;

				//if (dirs.pathingMode == PathingMode.TweenAlongPoints) { }

				PathFollowOutput output = MotionPlanning.FollowPath(_path, ref pfs, position, spd);

				if (npc) {
					inputs.hasMove   = true;
					inputs.move      = output.direction;
					inputs.moveSpeed = output.speed.GetValueOrDefault(spd);
				} else if (plr) {
					inputs.move      = output.direction;
					inputs.moveSpeed = output.speed.GetValueOrDefault(spd);
				}

				_state = pfs;

				if (output.reached_target) {
					inputs.NoMovement();

					if (_path.Nodes.Count > 0)
						actor.Teleport(pfs.GetTargetNode(_path).point);

					if (_state.region_path)
						PausePathing();
					else
						EndPathing();
				}
			}

			end:

			switch (LookMode)
			{
				case LookMode.Forward:
					_lookDir = inputs.move.normalized;
					break;

				case LookMode.Backward:
					_lookDir = -inputs.move.normalized;
					break;

				default:
					LookMode = LookMode.None;
					break;
			}

			switch (LookMode)
			{
				case LookMode.None:
					inputs.look = null;
					break;

				case LookMode.Forward:
					inputs.Look2D(_lookDir.xz().normalized);
					inputs.LookDirLerp = 0.15f;
					break;

				case LookMode.Backward:
					inputs.Look2D(_lookDir.xz().normalized);
					inputs.LookDirLerp = 0.15f;
					break;

				case LookMode.Position:
					inputs.Look2D(_lookDir.xz() - actor.transform.position.xz());
					inputs.LookDirLerp = 0.5f;
					break;

				case LookMode.Direction:
					inputs.Look2D(_lookDir.xz().normalized);
					inputs.LookDirLerp = 0.5f;
					break;

				case LookMode.WorldPoint:
					if (_lookPoint.TryGet(out Vector3 pos))
					{
						inputs.Look2D(pos.xz() - actor.transform.position.xz());
						inputs.LookDirLerp = 0.5f;
					}

					break;
			}


			/*if (/*WaitTimer > 0 ||#1# !states.ContainsKey(character)) {
				inputs.NoMovement();
				return;
			}

			var state = states[character];

			var result = MotionPlanning.Pathing_UpdateState(ref state, character.transform.position);

			if(state.state == MPState.Running)
				character.LUA_OnPathUpdate(result, state);

			if (result.reached_node) {
				character.LUA_OnPathReachNode(result, state);
			}

			inputs.move 		= new Vector2(state.follower_dir.x, state.follower_dir.z);
			inputs.hasMove 		= true;
			inputs.moveSpeed 	= state.follower_speed;

			state.distance_traveled += Vector3.Distance(transform.position, transform.position + (state.follower_dir * state.follower_speed * Time.deltaTime));

			states[character] = state;*/
		}


		//	 API
		//------------------------------------------------
		public void PausePathing()
		{
			_active = false;
			//directions.path            = null;
			//directions.pathCalculating = false;
		}

		public void EndPathing()
		{
			_active              = false;
			_path                = null;
			_calculating         = false;
			_state				 = PathFollowState.Default;
		}

		public WaitableUniTask follow_path(string region_path, int startIndex = 0, int targetIndex = 0, PathFollowMode mode = PathFollowMode.Calculated)
			=> new WaitableUniTask(FollowPath(region_path, startIndex, targetIndex, mode));


		// NOTE: Needs cleaning up, not usable yet

		public async UniTask FollowPath(string region_path, int startIndex = 0, int targetIndex = 0, PathFollowMode mode = PathFollowMode.Calculated)
		{
			_rpath = (await RegionController.GetRegionObject(region_path)) as RegionPath;
			if (_rpath == null) return;

			FollowPath(_rpath, startIndex, targetIndex, mode, Config.Value.loop);
		}

		public async void FollowPath(RegionPath path, int startIndex = 0, int targetIndex = 0, PathFollowMode mode = PathFollowMode.Calculated, bool loop = false)
		{
			_state   = new PathFollowState(startIndex, targetIndex) {region_path = true, follow_mode = mode, loop = loop};
			await StartRegionCalc(path);
			_active  = true;
		}

		public async void TeleportToPath(RegionPath path, int index, PathFollowMode mode = PathFollowMode.Calculated)
		{
			actor.Teleport(path.GetWorldPoint(index));
			_state = new PathFollowState(index, index) {region_path = true, follow_mode = mode, on_path = _state.on_path};
			await StartRegionCalc(path);
		}

		public void WalkToPreviousPathPoint(int number)
		{
			_active                           =  true;
			_state.target_index -= Mathf.Max(number, 1);
		}

		public void WalkToNextPathPoint(int number)
		{
			_active                           =  true;
			_state.target_index += Mathf.Max(number, 1);
		}

		public void WalkToPathEnd()
		{
			_active                         = true;
			_state.target_end = true;
		}

		public void WalkToPathIndex(int index)
		{
			_active                           = true;
			_state.target_index = index;
		}

		/*public void TweenToNextPathPoint(int number)
		{
			directions.moveMode               =  MoveMode.TweenAlongPath;
			_state.target_index += Mathf.Max(number, 1);
			_state.just_started =  true;
		}

		public void TweenToPreviousPathPoint(int number)
		{
			directions.moveMode               =  MoveMode.TweenAlongPath;
			_state.target_index -= Mathf.Max(number, 1);
			_state.just_started =  true;
		}*/

		public async UniTask StartRegionCalc(RegionPath rpath)
		{
			_path            = null;
			_calculating = true;
			//directions.regionPath = rpath;

			(MPPath path, bool ok) = await MotionPlanning.CalcRegionPath(rpath);

			_calculating = false;
			_path        = ok ? path : null;
		}

		public          void ResetInputs(Actor character, ref CharacterInputs inputs) {}
		public override void OnTick(float      dt) {}

		public override void Lua_Message(string msg, params DynValue[] inputs)
		{
			/*switch (msg) {
				case "wait":
					if (inputs.Length > 0 && inputs[0].Type == DataType.Number)
						WaitTimer = (float)inputs[0].Number;

					break;
			}*/
		}

		void OnDrawGizmos()
		{
			if (_active && _path != null)
				MotionPlanning.DrawPathInEditor(_path);
		}
	}
}