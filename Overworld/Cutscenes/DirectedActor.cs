using Anjin.Actors;
using Anjin.Cameras;
using Anjin.MP;
using Anjin.Regions;
using Anjin.Scripting;
using Anjin.Scripting.Waitables;
using Anjin.Util;
using DG.Tweening;
using DG.Tweening.Core;
using MoonSharp.Interpreter;
using UnityEngine;
using Util.UniTween.Value;

namespace Overworld.Cutscenes
{
	public enum LookMode
	{
		None,
		Forward,
		Backward,
		Direction,
		Position,
		CamRelative,
		WorldPoint
	}

	/// <summary>
	/// An actor that can be directed by a coroutine player.
	/// </summary>
	[LuaUserdata]
	public class DirectedActor : DirectedBase
	{
		public readonly string actorPath;

		public Actor      actor;
		public Directions directions;

		/*public RegionPath regionPath;
		public int        regionPathIndex;
		public int        regionPathTargetIndex;
		public Vector3    regionPathPrevTarget;*/

		public Tween   tween;
		public string  idleAnimation, walkAnimation, jumpAnimation;
		public bool    isAdjustingLook;
		public bool    guest;
		public Table   bubbleSettings;

		public Vector3?		spawnPosition;
		public Quaternion?	spawnRotation;

		private bool     _spawned;
		private Coplayer _coplayer;

		public Vector3 position => actor.position;

		/// <summary>
		/// Create from an actor path (using the ActorRegistry)
		/// </summary>
		public DirectedActor(string path, Table options = null) : base(options)
		{
			actorPath = path;

			// TODO (C.L.):
			// Do we need this at this point? We could have a fallback of
			// just trying to use the path as the address, which would be more streamlined.

			if (address == null && AssetsLua.TryGetActorAssetAddress(path, out var _path)) {
				address = _path;
			}
		}

		/// <summary>
		/// Create from an existing actor.
		/// </summary>
		public DirectedActor(Actor actor, Table options = null) : base(options)
		{
			gameObject = actor.gameObject;
			this.actor = actor;

			options.TryGet("is_guest", out guest, guest);
		}

		public override void ReadOptions(Table options)
		{
			base.ReadOptions(options);
			if (options == null) return;

			if (options.TryGet("target", out DynValue target))
			{
				initialTarget  = target;
				walkToPosition = true;
			}
		}

		public override void OnStart(Coplayer coplayer, bool auto_spawn = true)
		{
			if (started)
				return;

			base.OnStart(coplayer, auto_spawn);

			_coplayer = coplayer;

			DiscoverActor();

			if (gameObject == null)
			{
				Debug.LogError($"Could not get or spawn an actor for {actorPath}! This will break the script.");
				return;
			}

			// Control the actor (if set)
			if (controlFlag)
				Control();

			if (auto_spawn && initialPosition != null && !initialPosition.IsNil())
			{
				Spawn(initialPosition, options);
			}

			// Move to the initial position (if set)
			if (controlling && walkToPosition)
			{
				if (DynValueToCoords(initialTarget, out Vector3 pos, out Quaternion rot))
				{
					WalkToPoint(pos);
					directions.moveAnimation = walkAnimation;
				}

				coplayer.coroutine.OutsideWait(new ManagedActorMove(this));
			}

			if (actor != null) {
				spawnPosition = actor.transform.position;
				spawnRotation = actor.transform.rotation;
			}
		}

		public void Rediscover()
		{
			if(actor == null)
				DiscoverActor();
		}

		public void DiscoverActor()
		{
			bool wasDespawned = actor == null || gameObject == null;

			// Try getting through the ActorRegistry
			// ----------------------------------------
			Actor existing = ActorRegistry.FindByPath(actorPath) ?? ActorRegistry.Get(actorPath);
			if (gameObject == null && existing != null && !forceSpawn)
			{
				alreadyExisted = true;
				actor          = existing;
				gameObject     = existing.gameObject;
				ApplyCoplayerTags(_coplayer);
				Debug.Log($"[TRACE] StartUsing {existing} -> existing gameObject", existing);
			}

			// Instantiate the actor (if needed)
			// ----------------------------------------
			if (gameObject == null && loadedPrefab != null)
			{
				gameObject                    = Object.Instantiate(loadedPrefab, _coplayer.transform, true);
				gameObject.transform.position = Vector3.zero;

				actor = gameObject.GetComponent<Actor>();

				_spawned = true;
				Debug.Log($"[TRACE] StartUsing {gameObject} -> instantiate addressable prefab", gameObject);

				ApplyCoplayerTags(_coplayer);
			}

			if (wasDespawned && controlFlag)
				Control(force: true);
		}

		public void PollInputs(Actor character, ref CharacterInputs inputs)
		{
			ref Directions dirs = ref directions;

			switch (dirs.moveMode)
			{
				case MoveMode.None: {
					inputs.NoMovement();
					break;
				}

				case MoveMode.Direction: {
					inputs.move      = dirs.moveDirection.normalized;
					inputs.moveSpeed = dirs.moveSpeed;
					break;
				}

				case MoveMode.Pathing: {
					//If we have no paths at all, and we aren't calculating, we need to end pathing to avoid errors.
					if (!dirs.pathCalculating && dirs.path == null)
					{
						EndPathing();
						return;
					}

					if (dirs.pathCalculating) return;

					MotionPlanning.DrawMPPathInEditorALINE(dirs.path, Color.red);

					PathFollowState pfs      = dirs.pathState;
					Vector3         position = actor.transform.position;

					NPCActor    npc = actor as NPCActor;
					PlayerActor plr = actor as PlayerActor;

					float spd = character.velocity.magnitude;

					if (npc) spd      = npc.CSettings.WalkSpeed;
					else if (plr) spd = plr.Settings.WalkRun.MaxSpeed;

					if (dirs.moveSpeed.HasValue)
						spd = dirs.moveSpeed.Value;

					// If we have no speed after all of that, this is a failsafe
					if (spd < Mathf.Epsilon) {
						spd = 3;
					}

					//if (dirs.pathingMode == PathingMode.TweenAlongPoints) { }

					PathFollowOutput output = MotionPlanning.FollowPath(dirs.path, ref pfs, position, spd);

					if (npc)
					{
						inputs.hasMove   = true;
						inputs.move      = output.direction;
						inputs.moveSpeed = output.speed.GetValueOrDefault(spd);
					}
					else if (plr)
					{
						inputs.move      = output.direction;
						inputs.moveSpeed = output.speed.GetValueOrDefault(spd);
					} else {
						inputs.hasMove   = true;
						inputs.move      = output.direction;
						inputs.moveSpeed = output.speed.GetValueOrDefault(spd);
					}

					dirs.pathState = pfs;

					if (output.reached_target)
					{
						inputs.NoMovement();

						if (dirs.path.Nodes.Count > 0)
							actor.Teleport(pfs.GetTargetNode(dirs.path).point);

						if (dirs.pathState.region_path)
							PausePathing();
						else
							EndPathing();
					}

					break;
				}

				case MoveMode.Tween: {
					if (tween == null)
					{
						tween = dirs.moveTween.ApplyTo(
							() => actor.gameObject.transform.position,
							v => actor.Teleport(v),
							dirs.moveGoal
						);
					}
					else
					{
						if (!tween.IsActive())
						{
							directions.moveMode = MoveMode.None;
							tween               = null;
						}
					}

					break;
				}

				case MoveMode.TweenAlongPath: {
					//If we have no paths at all, and we aren't calculating, we need to end pathing to avoid errors.
					if (!dirs.pathCalculating && dirs.path == null)
					{
						EndPathing();
						return;
					}

					if (dirs.pathCalculating) return;

					MotionPlanning.DrawMPPathInEditorALINE(dirs.path, Color.red);

					PathFollowState pfs      = dirs.pathState;
					Vector3         position = actor.transform.position;

					PathFollowOutput output = MotionPlanning.TweenAlongPath(dirs.path, ref pfs, position, dirs.moveTween, ActorPosGetter, ActorPosSetter);

					dirs.pathState = pfs;

					if (output.reached_target)
					{
						inputs.NoMovement();

						/*if (dirs.path.Nodes.Count > 0)
							actor.Teleport(pfs.GetTargetNode(dirs.path).point);*/

						if (dirs.pathState.region_path)
							PausePathing();
						else
							EndPathing();
					}
					break;
				}

				case MoveMode.Anchor: {
					if (directions.moveAnchor.TryGet(out var pos))
					{
						actor.gameObject.transform.position = pos;
					}

					break;
				}
			}

			LookMode lookMode = dirs.lookMode;
			if (dirs.moveMode != MoveMode.None)
			{
				switch (dirs.lookMode)
				{
					case LookMode.Forward:
						dirs.lookDirection = inputs.move.normalized;
						break;

					case LookMode.Backward:
						dirs.lookDirection = -inputs.move.normalized;
						break;

					default:
						lookMode = LookMode.None;
						break;
				}
			}

			switch (lookMode)
			{
				case LookMode.None:
					inputs.look = null;
					break;

				case LookMode.Forward:
					inputs.Look2D(dirs.lookDirection.xz().normalized);
					inputs.LookDirLerp = 0.15f;
					break;

				case LookMode.Backward:
					inputs.Look2D(dirs.lookDirection.xz().normalized);
					inputs.LookDirLerp = 0.15f;
					break;

				case LookMode.Position:
					inputs.Look2D(dirs.lookPosition.xz() - actor.transform.position.xz());
					inputs.LookDirLerp = 0.5f;
					break;

				case LookMode.Direction:
					inputs.Look2D(dirs.lookDirection.xz().normalized);
					inputs.LookDirLerp = 0.5f;
					break;

				case LookMode.WorldPoint:
					if (dirs.lookPoint.TryGet(out Vector3 pos))
					{
						inputs.Look2D(pos.xz() - actor.transform.position.xz());
						inputs.LookDirLerp = 0.5f;
					}

					break;

				case LookMode.CamRelative:
					if (dirs.lookOrdinal != Direction8.None) {

						// TODO
						//float rot = GameCams.Live.UnityCam.transform.rotation.y;

						//inputs.Look2D();
					}

					break;
			}

			isAdjustingLook = lookMode != LookMode.None && inputs.look.HasValue && (inputs.look.Value.xz() - character.facing.xz()).sqrMagnitude > 0.1f;
		}

		public override void OnStop(Coplayer coplayer)
		{
			if (controlling)
				Uncontrol();

			if (actor != null && reset_transform) {
				if(spawnPosition.HasValue)
					actor.Teleport(spawnPosition.Value);

				if(spawnRotation.HasValue)
					actor.Reorient(spawnRotation.Value);
			}

			_coplayer = null;

			// This function did essentially the same thing
			idleAnimation = "idle";
			walkAnimation = "walk";
			jumpAnimation = "jump";
			directions    = Directions.@default;
			/*regionPath      = null;
			regionPathIndex = 0;

			regionPathTargetIndex = 0;*/

			tween?.Kill();
			tween = null;

			isAdjustingLook = false;


			base.OnStop(coplayer);
		}

		public override void Release()
		{
			if (controlling)
			{
				actor.PopOutsideBrain(_coplayer.actorBrain);
			}

			if (guest)
			{
				ActorController.ReturnGuest(actor as NPCActor);
				_spawned   = false;
				gameObject = null;
				actor      = null;
				return;
			}

			if (!_spawned)
				return;

			Object.Destroy(gameObject);

			_spawned   = false;
			gameObject = null;
			actor      = null;
		}

		public DirectedActor Keep()
		{
			keepAfter = true;
			return this;
		}

		public DirectedActor Control(bool force = false)
		{
			if (force || started && !controlling)
			{
				_coplayer.actorBrain.actors[actor] = this;
				actor.PushOutsideBrain(_coplayer.actorBrain);
				controlling = true;
			}
			else
			{
				controlFlag = true;
			}

			return this;
		}

		public DirectedActor Uncontrol()
		{
			// Releasing control mid-execution
			if (started && controlling) {
				if (actor != null) {

					if (_coplayer.actorBrain.actors.TryGetValue(actor, out var _))
						_coplayer.actorBrain.actors[actor] = null;

					actor.PopOutsideBrain(_coplayer.actorBrain);
				}

				controlling = false;
			} else {
				controlFlag = false;
			}

			return this;
		}

		public void Reset()
		{
			if(actor is ActorKCC kcc) kcc.Reset();
		}

		static Vector3[] temp_spawn_points = new Vector3[800];

		// Moved this into a seperate function so I could call it from a script on cutscene load.
		public DirectedActor Spawn(DynValue _position, Table _options = null)
		{
			if (_position == null || _position.IsNil() || _position.IsVoid()) _position = initialPosition;

			if (_options == null)
				_options = this.options;

			if (_position == null) return this;

			DynValue position = _position;
			Table    options  = _options;

			if (_position.Type == DataType.Table) { }

			switch (position.Type)
			{
				// TODO: Integrate region paths for various things.
				case DataType.String:
					string name = position.String;
					if (position.String[0] == ':')
						// Let's not do this : syntax, I think it's unnecessarily explicit
						name = position.String.Substring(1);

					if (_coplayer != null && _coplayer.state.graph.TryGet("_asset", out RegionGraph graph))
					{
						RegionObject graphObj = graph.FindByPath(name);
						switch (graphObj)
						{
							case RegionPath rpath:

								int            spawnIndex = 0;
								PathFollowMode mode       = PathFollowMode.Calculated;
								if (options != null)
								{
									options.TryGet("index",       out spawnIndex, spawnIndex);
									options.TryGet("follow_mode", out mode, mode);
								}

								TeleportToPath(rpath, spawnIndex, mode);
								//directions.regionPath = rpath;
								//directions.pathState  = new PathFollowState(0,0);
								/*regionPath            = rpath;
								regionPathIndex       = 0;*/
								//actor.Teleport(rpath.GetWorldPoint(0));
								break;

							case RegionObjectSpatial spatial:
								// TODO: Make this work somehow.
								/*if (spatial is RegionShape2D shape && spawnDistributed && shape.TryGetMetadata(out PointDistributionMetadata points)) {
									var num = points.GetFor(shape, temp_spawn_points, points.number);
								} else */
							{
								actor.Teleport(spatial.Transform.Position);
								actor.Reorient(spatial.Transform.Rotation);
							}
								break;
						}
					}

					break;

				case DataType.UserData:
					UserData ud = position.UserData;

					if (ud.TryGet(out Actor target_actor))
						actor.Teleport(target_actor.transform.position);
					else if (ud.TryGet(out DirectedBase directed) && directed.gameObject != null)
						actor.Teleport(directed.gameObject.transform.position);
					else
						actor.Teleport(position);

					break;

				default:
					actor.Teleport(position);
					break;
			}

			spawnPosition = actor.transform.position;

			return this;
		}

		public bool DynValueToCoords(DynValue position, out Vector3 pos, out Quaternion rot)
		{
			pos = actor.transform.position;
			rot = Quaternion.identity;

			switch (position.Type)
			{
				// TODO: Integrate region paths for various things.
				case DataType.String:
					string name = position.String;
					if (position.String[0] == ':')
						name = position.String.Substring(1);

					if (_coplayer != null && _coplayer.state.graph.TryGet("_asset", out RegionGraph graph))
					{
						RegionObject graphObj = graph.FindByPath(name);
						switch (graphObj)
						{
							case RegionPath rpath:
								pos = rpath.GetWorldPoint(0);
								return true;

							case RegionObjectSpatial spatial:
								pos = spatial.Transform.Position;
								rot = spatial.Transform.Rotation;
								return true;
						}
					}

					break;

				case DataType.UserData:
					switch (position.UserData.Object)
					{
						case Vector3 v:
							pos = v;
							return true;
						case Transform t:
							pos = t.position;
							rot = t.rotation;
							return true;
						case GameObject g:
							pos = g.transform.position;
							rot = g.transform.rotation;
							return true;
					}

					break;
			}

			return false;
		}

		private DOGetter<Vector3> ActorPosGetter => getPos;
		private DOSetter<Vector3> ActorPosSetter => setPos;

		private Vector3 getPos()            => actor.transform.position;
		private void    setPos(Vector3 pos) => actor.Teleport(pos);

		public async void StartPathCalc(Vector3 goal)
		{
			directions.path            = null;
			directions.pathCalculating = true;

			(MPPath path, bool ok) = await MotionPlanning.CalcPath(actor.transform.position, goal);

			directions.moveGoal        = goal;
			directions.pathCalculating = false;
			directions.path            = ok ? path : null;
		}

		public async void StartRegionCalc(RegionPath rpath)
		{
			directions.path            = null;
			directions.pathCalculating = true;
			directions.regionPath      = rpath;

			(MPPath path, bool ok) = await MotionPlanning.CalcRegionPath(rpath);

			directions.pathCalculating = false;
			directions.path            = ok ? path : null;
		}

		public void PausePathing()
		{
			directions.moveMode = MoveMode.None;
			//directions.path            = null;
			//directions.pathCalculating = false;
		}

		public void EndPathing()
		{
			directions.moveMode   = MoveMode.None;
			directions.path       = null;
			directions.regionPath = null;

			directions.pathCalculating = false;
			directions.pathState       = PathFollowState.Default;
		}

		public void WalkToPoint(Vector3 point)
		{
			if (directions.moveMode == MoveMode.Pathing)
				EndPathing();

			StartPathCalc(point);
			directions.moveMode             = MoveMode.Pathing;
			directions.pathState            = PathFollowState.Default;
			directions.pathState.target_end = true;
		}

		public void TweenPath(RegionPath path, TweenerTo tweener, int? startIndex = null, int? targetIndex = null, PathFollowMode mode = PathFollowMode.Calculated)
		{
			directions.moveMode  = MoveMode.TweenAlongPath;
			directions.moveTween = tweener;

			int start, target;
			start  = startIndex.GetValueOrDefault(directions.pathState.index);
			target = targetIndex.GetValueOrDefault(directions.pathState.target_index);

			directions.pathState = new PathFollowState(start, target) {region_path = true, follow_mode = mode};
			StartRegionCalc(path);
		}

		public void FollowPath(RegionPath path, int startIndex = 0, int targetIndex = 0, PathFollowMode mode = PathFollowMode.Calculated)
		{
			directions.moveMode    = MoveMode.Pathing;
			directions.pathingMode = PathingMode.RegionPath;
			directions.pathState   = new PathFollowState(startIndex, targetIndex) {region_path = true, follow_mode = mode};
			StartRegionCalc(path);
		}

		public void TeleportToPath(RegionPath path, int index, PathFollowMode mode = PathFollowMode.Calculated)
		{
			if (path.Points.Count <= 0) {
				actor.Teleport(path.Transform.Position);
				return;
			}

			actor.Teleport(path.GetWorldPoint(index));
			directions.pathState = new PathFollowState(index, index) {
				region_path = true,
				follow_mode = mode,
				on_path		= true,
			};

			StartRegionCalc(path);
		}

		public void WalkToPreviousPathPoint(int number)
		{
			directions.moveMode = MoveMode.Pathing;
			directions.pathState.IncrementTargetIndex(-Mathf.Max(number, 1), directions.path);
		}

		public void WalkToNextPathPoint(int number, PathFollowMode? mode = null)
		{
			directions.moveMode =  MoveMode.Pathing;
			directions.pathState.IncrementTargetIndex(Mathf.Max(number, 1), directions.path);

			if (mode != null)
				directions.pathState.follow_mode = mode.Value;
		}

		public void WalkToPathEnd()
		{
			directions.moveMode             = MoveMode.Pathing;
			directions.pathState.target_end = true;
		}

		public void WalkPathLoop(int count = -1)
		{
			directions.moveMode       = MoveMode.Pathing;
			directions.pathState.loop		= true;
			directions.pathState.loop_count = count;
		}

		public void WalkToPathIndex(int index)
		{
			directions.moveMode               = MoveMode.Pathing;
			directions.pathState.target_index = index;
		}

		public void TweenToNextPathPoint(int number, PathFollowMode? mode = null)
		{
			directions.moveMode               =  MoveMode.TweenAlongPath;
			directions.pathState.target_index += Mathf.Max(number, 1);
			directions.pathState.just_started =  true;

			if (mode != null)
				directions.pathState.follow_mode = mode.Value;
		}

		public void TweenToPreviousPathPoint(int number)
		{
			directions.moveMode               =  MoveMode.TweenAlongPath;
			directions.pathState.target_index -= Mathf.Max(number, 1);
			directions.pathState.just_started =  true;
		}

		public void TeleportToNextPathPoint(int number, PathFollowMode mode = PathFollowMode.Raw)
		{
			directions.pathState.target_index += Mathf.Max(number, 1);
			actor.Teleport(directions.path.GetNode(directions.pathState.target_index).point);
		}

		public void TeleportToPrevPathPoint(int number, PathFollowMode mode = PathFollowMode.Raw)
		{
			directions.pathState.target_index -= Mathf.Max(number, 1);
			actor.Teleport(directions.path.GetNode(directions.pathState.target_index).point);
		}

		public void CompleteMove()
		{
			if (directions.moveMode == MoveMode.None) return;

			switch (directions.moveMode)
			{
				case MoveMode.Pathing:
					if (directions.path != null) {
						var node = directions.pathState.GetTargetNode(directions.path);
						actor.Teleport(node.point);
					} else if (directions.regionPath != null) {
						var pt = directions.regionPath.GetWorldPoint(directions.pathState.target_index);
						actor.Teleport(pt);
					} else {
						actor.Teleport(directions.moveGoal);
					}

					directions.pathState.index = directions.pathState.target_index;

					if (directions.pathState.region_path)
						PausePathing();
					else
						EndPathing();
					break;

				case MoveMode.Tween:
					tween.Complete();
					break;

				case MoveMode.Direction:
					// TODO: Is this even possible to do? And are we actually going to use move_dir in places we need to skip?
					break;
			}

			directions.moveMode = MoveMode.None;
		}

		public override string ToString() => $"DirectedActor(go: {(gameObject != null ? gameObject.name : "N/A")}, path: {actorPath}, pfb: {loadedPrefab})";


		public enum MoveMode
		{
			None,
			Direction,
			Pathing,
			Tween,
			TweenAlongPath,
			Anchor,
		}

		public enum PathingMode
		{
			ToPoint,
			RegionPath,
			TweenAlongPoints
		}

		/// <summary>
		/// Directions for a controllable actor to follow.
		/// </summary>
		public struct Directions
		{
			// Looking
			public LookMode   lookMode;
			public Vector3    lookPosition;
			public Vector3    lookDirection;
			public WorldPoint lookPoint;
			public Direction8 lookOrdinal;

			// Animations
			public bool        overrideAnimEnabled;
			public RenderState overrideAnimState;
			public bool        repeats;
			public bool        pauseAtEnd;

			// Movement
			public MoveMode   moveMode;
			public Vector3    moveDirection;
			public Vector3    moveGoal;
			public float?     moveSpeed;
			public TweenerTo  moveTween;
			public string     moveAnimation;
			public bool       moveAnimationRiseFall;
			public WorldPoint moveAnchor;

			public bool navigating;

			public PathingMode pathingMode;
			public MPPath      path;
			public RegionPath  regionPath;

			public PathFollowState pathState;
			public bool            pathCalculating;
			//public bool            waitingOnCalc;

			public static Directions @default = new Directions
			{
				lookPoint           = new WorldPoint(),
				lookOrdinal			= Direction8.None,
				overrideAnimEnabled = false,
				pauseAtEnd          = false,

				moveMode      = MoveMode.None,
				moveDirection = Vector3.zero,
				moveSpeed     = null,
				moveAnchor    = WorldPoint.Default,

				path            = null,
				pathState       = PathFollowState.Default,
				pathCalculating = false,
				//waitingOnCalc   = false,

				navigating = false,
				moveGoal   = Vector3.zero,

				overrideAnimState = new RenderState(Anjin.Actors.AnimID.Stand)
			};

			public void LookReset()
			{
				lookMode = LookMode.None;
			}

			public void LookPosition(Vector3 pos)
			{
				lookMode     = LookMode.Position;
				lookPosition = pos;
			}

			public void LookForward()
			{
				lookMode = LookMode.Forward;
			}

			public void LookBackward()
			{
				lookMode = LookMode.Backward;
			}

			public void LookCam(Direction8 direction)
			{
				lookMode    = LookMode.CamRelative;
				lookOrdinal = direction;
			}

			public void LookDirection(Vector3 direction)
			{
				lookMode      = LookMode.Direction;
				lookDirection = direction;
			}

			public void LookWorldPoint(WorldPoint point)
			{
				lookMode  = LookMode.WorldPoint;
				lookPoint = point;
			}
		}
	}
}