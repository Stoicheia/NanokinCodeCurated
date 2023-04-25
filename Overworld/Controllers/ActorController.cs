using System;
using System.Collections.Generic;
using Anjin.Audio;
using Anjin.Cameras;
using Anjin.MP;
using Anjin.Nanokin;
using Anjin.Nanokin.Map;
using Anjin.Nanokin.ParkAI;
using Anjin.Scripting;
using Anjin.UI;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using Data.Overworld;
using Drawing;
using ImGuiNET;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using Pathfinding;
using SaveFiles;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using Util;
using Util.Components;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;
using Flags = Anjin.Core.Flags.Flags;
using Random = System.Random;
using g = ImGuiNET.ImGui;

namespace Anjin.Actors
{
	/// <summary>
	/// Offers functionalities related to
	/// - player party actors
	/// - player active actor
	/// - player active camera
	/// - guest npcs
	/// </summary>
	[DefaultExecutionOrder(-15)]
	public class ActorController : StaticBoy<ActorController>, IDebugDrawer, IDrawGizmos
	{
		public const int GUEST_POOL_INIT_SIZE = 16;
		public const int GUEST_POOL_MAX_SIZE  = 1024;

		// PLAYER CONTROL
		// The player can take control any controllableactor, regardless if that actor is actually controllable
		// by the player in any meaningful way.
		//
		// CAMERA CONTROL
		//
		//
		// We don't use actor references for which actor the player is controlling because it needs to be one
		// object, anything accessing it will need to be fast, and there are too many edge cases with a softly resolved
		// reference like an ActorRef.
		//-------------------------------------

		//The controllable actor the player is currently controlling.
// @formatter:off
		[FormerlySerializedAs("SpawnParty")]
		[Title("Settings")]
		public bool            ShouldSpawnParty = true;

		[Title("References")]
		public Transform       PartyTransform;
		public TransitionBrain TransitionBrain;
		public PlayerCameraRig PlayerCameraPrefab;

		[Title("Debug")]
		[DebugVars]
		[NonSerialized] public static bool isSpawned = false;
		[NonSerialized] public static bool isPartySpawned = false;
		[NonSerialized] public static bool playerActive = false;
		[NonSerialized] public static Actor 				playerActor = null;
		[NonSerialized] public static PlayerControlBrain    playerBrain = null;
		[NonSerialized] public static PlayerCameraRig 		playerCamera = null;
		[NonSerialized] public static List<Actor> 			partyActors = null;

		[Space]

		private Random                           _rand = new Random();
		private ComponentPool<NPCActor>          _guestPool;
		private int                              _currentPartyActor = 0;
// @formatter:on

		public static AsyncLazy initTask;

		[ShowInPlay] public static ActorEvent OnControllingActorChanged;
		[ShowInPlay] public static GameEvent  OnInmapWarp;

		[ShowInPlay] public static Vector3? PlayerPosition;
		[ShowInPlay] public static Vector3? PlayerFacing;

		public const int       STABLE_GROUNDING_SLOTS = 10;

		public float StableGroundingTimer = 1;

		[ShowInPlay] static        Vector3[] _stableGroundPoints    = new Vector3[STABLE_GROUNDING_SLOTS];
		[ShowInPlay] public static Vector3?  PlayerGroundPosition;

		[ShowInPlay] public static Vector3?  LastStableStandingPosition;
		[ShowInPlay] public static Vector3?  LastStablePlayerFacingDirection;

		/// <summary>
		/// Temporarially keep the stable position vars from being reset when the player despawns.
		/// This is intended mainly for the game controller to be able to set this while respawning from an overworld death.
		/// </summary>
		public static bool LockStablePosition = false;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			playerActor               	= null;
			playerActive              	= false;
			partyActors               	= null;
			playerBrain               	= null;
			isSpawned                 	= false;
			isPartySpawned            	= false;
			OnInmapWarp               	= null;
			OnControllingActorChanged 	= null;
			PlayerPosition            	= null;
			LockStablePosition			= false;
		}

		public ActorController()
		{
			DrawingManager.Register(this);
		}

		protected override void OnAwake()
		{
			base.OnAwake();

			partyActors = new List<Actor>(4);
			_rand       = new Random();

			_guestPool = new ComponentPool<NPCActor>(gameObject.AddChild("Guest NPC Pool").transform, GameAssets.Live.GuestPrefab)
			{
				maxSize      = GUEST_POOL_MAX_SIZE,
				onAllocating = OnGuestAllocated,
				allocateTemp = true,
				overrideTags = true,
			};

			playerBrain  = GetComponent<PlayerControlBrain>();
			playerCamera = Instantiate(PlayerCameraPrefab, transform, false);
		}

		private void OnDestroy()
		{
			_guestPool.Destroy();
		}

		private void OnGuestAllocated(NPCActor obj)
		{
			ActorDesigner design = obj.designer;
			(var bodySheet, var headSheet, PeepDef peep) = PeepGenerator.MakePeep();

			design.Mode = ActorDesigner.Modes.BodyHead;

			design.Body      = bodySheet;
			design.Head      = headSheet;
			design.HairColor = GameAssets.Live.PeepSpriteDatabase.HairProfiles.RandomElement(_rand);
			design.SkinColor = GameAssets.Live.PeepSpriteDatabase.SkinProfiles.RandomElement(_rand);

			design.WriteParts();
			design.Rig.Spawn();
		}

		private void Start()
		{
			//initTask = UniTask.Lazy(InitAsync);
			//if (!GameOptions.current.load_on_demand)
				//initTask.Task.Forget(); // Trigger this task immediately

			DebugSystem.Register(this);
		}

		public async UniTask InitializeThroughGameController() => await InitAsync();

		private async UniTask InitAsync()
		{
			// Init the guest pool
			if (!GameOptions.current.pool_on_demand)
			{
				_guestPool.prefab = GameAssets.Live.GuestPrefab;
				_guestPool.AllocateAdd(GUEST_POOL_INIT_SIZE);
			}
		}

		// SET ACTIVE
		// ----------------------------------------


		/// <summary>
		/// Set the actor that the player should be able to control.
		/// If set to null, the default controllable actor will be used.
		/// </summary>
		/// <param name="actor"></param>
		public static void SetPlayer([NotNull] Actor actor, bool updateCamera = true)
		{
			// Cleanup
			// ----------------------------------------
			if (playerActor != null)
				playerActor.PopOutsideBrain(playerBrain);


			// Assign
			// ----------------------------------------
			playerActor  = actor;
			playerActive = actor != null;

			if(playerActive) {
				actor.PushOutsideBrain(playerBrain);

				if(updateCamera) {
					playerCamera.SetActor(actor);
					playerCamera.Teleport(actor.Position, actor.facing);
				}
			}

			AudioManager.Live.UpdateZonesInRange(playerActor);
			OccluderSystem.SetReferencePoint(GameCams.Live.UnityCam.transform);

			OnControllingActorChanged?.Invoke(actor);
		}

		public static void SetPlayerToDefault()
		{
			// NOTE (C.L. 01-21-2023): No idea if this will present issues
			SetPlayer(partyActors[0]);
		}

		public static void SetPlayerPartyActive(bool state)
		{
			Live.PartyTransform.gameObject.SetActive(state);
		}


		// SPAWNING
		// ----------------------------------------

		[CanBeNull]
		public static Actor SpawnPlayer(Spawn spawn)
		{
			if (isSpawned)
			{
				Debug.LogError("Cannot spawn because the player is already spawned.");
				return null;
			}

			// Get the actor to spawn
			// ----------------------------------------
			Actor prefab = spawn.prefab;
			if (prefab == null)
				prefab = GameAssets.Live.BaseSpawnableCharacter;

			// Get the spawn position
			// ----------------------------------------
			Actor player = Instantiate(
				prefab,
				spawn.position,
				Quaternion.LookRotation(spawn.facing.magnitude > 0 ? spawn.facing : Vector3.zero),
				Live.PartyTransform);

			ActorRegistry.Register(player);
			partyActors.Add(player);
			isSpawned = true;

			Lua.RunGlobal("on_player_spawn", new object[] {new DirectedActor(player)}, true);


			// TODO(C.L.):
			OverworldHUD.Live.OnSpawnPlayer();
			//OverworldHUD.Live.ToggleUIState(1);


			return player;
		}

		[Button, ShowInPlay]
		[LuaGlobalFunc("spawn_party_members")]
		public static void SpawnPartyMembers(Actor player = null)
		{
			if (isPartySpawned) return;
			if (player == null)
			{
				if (!isSpawned)
				{
					Debug.LogError("ActorController: can't spawn without a player.");
					return;
				}

				player = playerActor;
			}

			PartyLeader leader = player.GetComponent<PartyLeader>();
			if (leader == null)
			{
				Debug.LogWarning("Cannot spawn party members because the player is not a PartyLeader.");
				return;
			}

			isPartySpawned = true;

			if (!Flags.GetBool("party_any")) {
				Debug.Log("Spawning no party members (flag 'party_any' is disabled).");
				return;
			}

			SpawnMember(Character.Jatz, "party_jatz");
			SpawnMember(Character.Serio, "party_serio");
			SpawnMember(Character.Peggie, "party_peggie");

			void SpawnMember(Character character, string flag)
			{
				if (!Flags.GetBool(flag)) return;

				if (!GameAssets.Live.LoadedPartyMemberPrefabs.TryGetValue(character, out NPCActor prefab)) {
					Debug.LogError($"Could not spawn party member for character {character}, no prefab loaded by GameAssets.");
					return;
				}

				Actor actor = Instantiate(prefab, player.transform.position, Quaternion.identity, Live.PartyTransform);

				if (!actor) {

					Debug.LogError($"Failed to spawn party member for character {character}. Prefab exists but did not spawn for some reason.");
					return;
				}

				partyActors.Add(actor);
				PartyMemberBrain member = actor.GetComponent<PartyMemberBrain>();
				if (member) leader.AddPartyMember(member);

			}
		}

		// Spawn the full party
		public static void SpawnParty(Spawn spawn)
		{
			Actor player = SpawnPlayer(spawn);
			if (player == null)
				return;

			SetPlayer(player);
			playerCamera.ReorientInstant(spawn.facing);

			if (Live.ShouldSpawnParty && GameOptions.current.ow_party)
			{
				SpawnPartyMembers(player);
			}
		}

		/// <summary>
		/// Try to despawn the current player.
		/// </summary>
		public static void EnsureDespawned()
		{
			if (!isSpawned)
			{
				//Debug.LogError("Cannot despawn because the player is not yet spawned.");
				return;
			}


			//SaveManager.current.Position = playerActor.Position;

			DespawnParty();
		}

		public static void DespawnParty()
		{
			SetPlayer(null);

			foreach (Actor actor in partyActors)
			{
				if (actor != null)
				{
					actor.actorActive = false;
					actor.gameObject.Destroy();
				}
			}

			isSpawned      = false;
			isPartySpawned = false;
			partyActors.Clear();
		}


		public static void SetPartyActive(bool active)
		{
			foreach (Actor actor in partyActors)
			{
				if (actor != null) {
					actor.gameObject.SetActive(active);
				}
			}
		}

		[Button, ShowInPlay]
		[LuaGlobalFunc("despawn_party_members")]
		public static void DespawnPartyMembers()
		{
			isPartySpawned = false;

			PartyLeader leader = playerActor.GetComponent<PartyLeader>();

			for (var i = 0; i < partyActors.Count; i++)
			{
				Actor actor = partyActors[i];
				if (actor != null && actor != playerActor)
				{
					PartyMemberBrain member = actor.GetComponent<PartyMemberBrain>();
					if (leader != null)
						leader.RemovePartyMember(member);

					actor.actorActive = false;
					actor.gameObject.Destroy();

					partyActors.RemoveAt(i);
					i--;
				}
			}
		}

		public static void RespawnPlayer()
		{
			// TODO(C.L.)

			// Figure out where we need to respawn the player. (last standing position, spawn point, ect)
			// Lock game so player can't do anything and nothing interferes (special game state?)

			// (Optional) async outro (player actor death animation, screen transition, ect)

			// Despawn current player actor and respawn in proper position

			// (Optional) async intro (incoming screen transition)

			// Return control to player
		}

		public static void LockPlayers(bool lockEnable)
		{
			if (!playerBrain) return;
			playerBrain.enabled = !lockEnable;
		}

		public static void TeleportPlayer(Vector3 position, Vector3 facing)
		{
			playerActor.Teleport(position);
			playerActor.Reorient(facing);
			playerCamera.Teleport(position, facing);
		}

		[LuaGlobalFunc("teleport_party_to_spawnpoint")]
		public static void TeleportPartyToSpawnpoint(SpawnPoint spawn)
		{
			if (spawn != null && spawn.MemberSpawnPoints.Count == 0) return;

			for (int i = 0; i < partyActors.Count; i++)
			{
				partyActors[i].Teleport(spawn, i, true);
			}
		}

		/// <summary>
		/// Warps the current player actor to the receiver with the specified ID.
		/// </summary>
		public bool WarpTo(int warpId)
		{
			if (!isSpawned) return false;

			WarpReceiver receiver = WarpReceiver.FindReceiver(warpId);
			if (receiver)
			{
				for (int i = 0; i < partyActors.Count; i++)
				{
					partyActors[i].Teleport(receiver.GetStartingPosition());
				}
			} else {
				return false;
			}

			OnInmapWarp?.Invoke();
			return true;
		}


		/// <summary>
		/// Rent a guest from the pool.
		/// </summary>
		/// <returns></returns>
		public static NPCActor RentGuest()
		{
			var actor = Live._guestPool.Rent();
			// TODO
			// if (ok) actor.charAnimator.SpriteAnimator.colorSetters.ForEach(x => x.UpdateMaterialProperties());
			// TODO we need to return the guests to the pool at some point

			return actor;
		}

		/// <summary>
		/// Get a number of random guests from the pool.
		/// </summary>
		public static List<NPCActor> GetRandomGuests(int number)
		{
			var (actors, ok) = Live._guestPool.Rent(number);

			if (ok)
			{
				for (int i = 0; i < actors.Count; i++)
				{
					// TODO
					// actors[i].charAnimator.SpriteAnimator.colorSetters.ForEach(x => x.UpdateMaterialProperties());

					NPCActor actor = actors[i];

					var designer = actor.designer;

					designer.SetRandomPeep();
					designer.Apply();
				}
			}

			// TODO we need to return the guests to the pool at some point
			return actors;
		}

		public static void ReturnGuest(NPCActor actor)
		{
			Live._guestPool.ReturnSafe(actor);
		}


		private void Update()
		{
			if (GameController.Live.CanControlPlayer())
			{
				// Change the party actor being control
				// ----------------------------------------
				if (GameInputs.IsPressed(Key.P))
				{
					_currentPartyActor++;
					if (_currentPartyActor >= partyActors.Count)
						_currentPartyActor = 0;

					SetPlayer(partyActors[_currentPartyActor]);
				}
			}

			if(!LockStablePosition) {
				if (playerActor) {
					PlayerPosition = playerActor.transform.position;
					PlayerFacing   = playerActor.transform.forward;

					PlayerGroundPosition = null;

					// Stable grounding
					if(playerActor is ActorKCC kcc) {
						if(kcc.IsGroundState) {
							var (info, ok) = MotionPlanning.GetPosOnNavmesh(kcc.Motor.GroundingStatus.GroundPoint, searchRadius: 0.25f);
							if (ok) {
								PlayerGroundPosition = info.position;
							}
						}
					}

				} else {
						PlayerPosition             = null;
						PlayerFacing               = null;
						PlayerGroundPosition       = null;
						LastStableStandingPosition = null;
				}
			}

			if (PlayerGroundPosition.HasValue) {
				LastStableStandingPosition = PlayerGroundPosition;
				if (PlayerFacing.HasValue) {
					LastStablePlayerFacingDirection = PlayerFacing;
				}
			}

			if (SaveManager.HasData) {
				SaveData data = SaveManager.current;

				// Insure level and standing position is always saved.
				if (GameController.ActiveLevel && GameController.ActiveLevel.Manifest)
					data.Location_Current.Level = GameController.ActiveLevel.Manifest.Level;
				else
					data.Location_Current.Level = LevelID.None;

				data.Location_Current.LastStableStandingPosition = LastStableStandingPosition;
				data.Location_Current.FacingDirection            = PlayerFacing;
			}



			if (DebugSystem.Opened) {
				if (PlayerGroundPosition.HasValue)
					Draw.ingame.WireSphere(PlayerGroundPosition.Value, 0.15f, Color.red);

				if (LastStableStandingPosition.HasValue)
					Draw.ingame.WireSphere(LastStableStandingPosition.Value, 0.14f, Color.blue);
			}
		}

		[LuaGlobalFunc("get_party_actors")]
		public static List<Actor> GetPartyActors() => partyActors;


		// TODO: This is a bit messy, could probably be simplified

		[LuaGlobalFunc("reorient_player_cams")]
		public static void ReorientPlayerCams(DynValue val)
		{
			if (val.IsVoid() || val.IsNil())
			{
				playerCamera.ReorientForward();
			}
			else if (val.AsTable(out Table tbl))
			{
				float? speed = null;
				tbl.TryGet("speed", out speed);

				if (tbl.TryGet("dir", out Vector3 direction))
				{
					if (speed == null || speed <= Mathf.Epsilon)
						playerCamera.ReorientInstant(direction);
					else
						playerCamera.Reorient(direction, speed);
				}
				else
				{
					if (speed == null || speed <= Mathf.Epsilon)
						playerCamera.ReorientForwardInstant();
					else
						playerCamera.ReorientForward(speed);
				}
			}
			else if (val.Type == DataType.UserData)
			{
				if (val.UserData.TryGet(out Vector3 dir))
					ReorientPlayerCams(dir);
			}
		}

		public static void ReorientPlayerCams(Vector3 dir, float? speed = null)
		{
			playerCamera.Reorient(dir);
		}

		public void OnLayout(ref DebugSystem.State state)
		{
			if (state.Begin("Actors")) {

				if(playerActor) {
					if (g.Button("Spawn Party Members"))	SpawnPartyMembers();
					if (g.Button("Despawn Party Members"))	DespawnPartyMembers();
				}

				if (g.BeginTabBar("tabs")) {

					if (g.BeginTabItem("Registry")) {
						g.Columns(2);
						foreach (KeyValuePair<string,Actor> pair in ActorRegistry.FullPathRegistry) {
							g.Text(pair.Key ?? "(null string)");
							g.NextColumn();

							if(pair.Value) {
								AImgui.Text(pair.Value.ToString(), ColorsXNA.LimeGreen);
							} else {
								AImgui.Text("(null actor)", Color.red);
							}
							g.NextColumn();
						}
						g.Columns(1);
						g.EndTabItem();
					}

					g.EndTabBar();
				}

			}
			g.End();
		}

		protected void OnDrawGizmos() { }

		public void DrawGizmos()
		{

		}
	}
}