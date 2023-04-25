#define PARKAI_DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Actors;
using Anjin.Cameras;
using Anjin.Nanokin;
using Anjin.Nanokin.Core;
using Anjin.Nanokin.Crowds;
using Anjin.Nanokin.ParkAI;
using Anjin.Regions;
using Anjin.Scripting;
using Anjin.UI;
using Anjin.Util;
using API.Spritesheet.Indexing;
using API.Spritesheet.Indexing.Runtime;
using Cysharp.Threading.Tasks;
using Drawing;
using ImGuiNET;
using JetBrains.Annotations;
using Overworld.Rendering;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Jobs;
using UnityEngine.Profiling;
using Util;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;
using g = ImGuiNET.ImGui;
using Random = System.Random;


namespace Overworld.Controllers
{
	public class ParkAIConfig
	{
		public int RNGSeed = 0;

		[Min(0)]
		public float SimSpeed = 1;
		public bool  UsePathfinding    = true;
		public float Peep_WalkSpeedMod = 1;
		public bool  UseAvoidance      = true;
		public bool  GlobalAvoidance   = false;

		// LODs
		public PeepLODReferencePoint LODReferencePoint = PeepLODReferencePoint.Camera;

		public float LOD_Dist_0 = 20;
		public float LOD_Dist_1 = 60;
		public float LOD_Dist_2 = 100;
		public float LOD_Dist_3 = 150;
		public float LOD_Dist_4 = 225;

		public PeepLOD LOD_Visible        = PeepLOD.LOD2;
		public PeepLOD LOD_Animating      = PeepLOD.LOD1;
		public PeepLOD LOD_GroundSnapping = PeepLOD.LOD1;
		public PeepLOD LOD_Emotes         = PeepLOD.LOD0;

		// Avatars
		public bool Avatars              = true;
		public bool AvatarAnimation      = true;
		public bool AvatarGroundSnapping = true;

		public bool EnableEmotes = true;

		// Buckets
		public bool UseBuckets = true;
		public int  BucketSize = 20;

		public float Peep_StatGainMod = 1;

		[Min(0)]
		public float AvoidanceRadius = 32f;

		public bool Debug_DrawAgentIDs      = false;
		public bool Debug_DrawAvoidanceInfo = false;
	}

	public enum PeepLODReferencePoint { Player, Camera }

	public enum ParkAIAnim
	{
		Stand,
		Walk,
		Sit
	}

	public struct AnimationSet
	{
		public AnimationBinding[] stand;
		public AnimationBinding[] walk;
		public AnimationBinding[] sit;
	}

	public class ParkAIPeep
	{
		public int           Bucket = -1;
		public PeepBehaviour Behaviour;
		public PeepLOD       LOD;

		public float     LODDistance;
		public PeepDef   Definition;
		public PeepStats Stats;

		//public Peep      Peep;
		public PeepAgent  Agent;
		public EmotePopup Emote;
		//public float      emoteTimer;

		public bool                        Visible;
		public bool                        HasAvatar;
		public bool                        Fixed;
		public ParkAIAvatar                Avatar;
		public ComponentPool<ParkAIAvatar> SourcePool;

		public Vector3 LookDirection;

		public ParkAIAnim Animation;
		public float      FrameTimer;
		public int        FrameIndex;
		public Direction8 FrameDir;

		public AnimationSet HeadFrames;
		public AnimationSet BodyFrames;

		// OLD
		public AIAvatarOld AvatarOld;

		public bool   UsingAvoidance = false;
		public ushort AvoidanceID    = 0;

		public Vector3 originalHeadLocalPos;
		public float   headOffset;

		public void ClearEmote()
		{
			if (!Emote) return;

			GameHUD.Live.emotePopupPool.ReturnSafe(Emote);
			Emote = null;
		}
	}


	public class ParkAIController : StaticBoy<ParkAIController>, IDebugDrawer, IRuntimeGraphHolder
	{
		public enum SystemState { Inactive, Running, Paused, Suspended }

		private const float AVATAR_ANIMATION_SPEED = 0.65f;

		[Title("State")]  public        SystemState  State;
		[ShowInInspector] public static ParkAIConfig Config;
		[Range(0, 10000)] public        int          AvatarNumber = 50;

		[Title("Runtime")]
		[HideInInspector]
		public List<ParkAIPeep> AllPeeps;

		/*[HideInEditorMode] public TickTimer NearTimer;
		[HideInEditorMode] public TickTimer FarTimer;*/
		[HideInEditorMode] public float TickTimescale = 1;

		[NonSerialized, ShowInPlay]
		public BurstAvoidanceSystem AvoidanceSystem;

		RuntimeParkAIGraph    _loadedGraph;
		BillboardJobManager   _billboardManager;
		Plane[]               _cam_frustum_planes;
		TransformAccessArray  _avatarTransforms;
		ParkAIAnimationSystem _animationSystem;
		//Bucket                  _decisionBuckets;
		LevelManifest              _sourceManifest;
		ComponentPool<AIAvatarOld> _avatarsOLD;

		private bool _ready;

		private Transform                   _avatarsRoot;
		private ComponentPool<ParkAIAvatar> _avatarsMale;
		private ComponentPool<ParkAIAvatar> _avatarsMaleRound;
		private ComponentPool<ParkAIAvatar> _avatarsFemale;
		private ComponentPool<ParkAIAvatar> _avatarsFemaleRound;
		private ComponentPool<ParkAIAvatar> _avatarsChild;

		private List<float> _scratchWeights;

		private BucketGroup _nearBuckets;
		private BucketGroup _farBuckets;
		private BucketGroup _aiBuckets;

		private List<Bucket> _buckets;
		private int          _bucketSize;
		private int          _bucketIndex;
		//private TickTimer    _bucketTimer;

#if PARKAI_DEBUG
		private ParkAIDebugInfo _debugInfo;
#endif

		//NOTE: All agents process within the same 'timeframe', so they can't create their own instances of Random, as they'd have the same seed.
		public Random _rand;

		protected override void OnAwake()
		{
			DebugSystem.Register(this);

			State = SystemState.Inactive;

			if (Config == null)
				Config = new ParkAIConfig();

			AllPeeps = new List<ParkAIPeep>();
			_buckets = new List<Bucket>();
			_avatarsOLD = new ComponentPool<AIAvatarOld>(gameObject.AddChild("Avatars Pool").transform)
				{ safetyChecks = false };

			_rand               = new Random(Config.RNGSeed);
			_billboardManager   = new BillboardJobManager();
			AvoidanceSystem     = new BurstAvoidanceSystem();
			_animationSystem    = new ParkAIAnimationSystem();
			_cam_frustum_planes = new Plane[6];
			_sourceManifest     = null;
			_scratchWeights     = new List<float>();


			PlayerLoopInjector.Inject<ParkAIController>(Anjin.Nanokin.Core.PlayerLoopTiming.PreLateUpdate, PreLateUpdate);

#if PARKAI_DEBUG
			_debugInfo = new ParkAIDebugInfo(0);
#endif
		}

		public async UniTask GlobalInit()
		{
			if (_ready) return;

			var root = gameObject.AddChild("Avatars Root");
			_avatarsRoot = root.transform;

			/*_avatarsMale        = await LoadAvatarType("ParkAI/AvatarMale", 300);
			_avatarsFemale      = await LoadAvatarType("ParkAI/AvatarFemale", 300);
			_avatarsMaleRound   = await LoadAvatarType("ParkAI/AvatarMaleRound", 300);
			_avatarsFemaleRound = await LoadAvatarType("ParkAI/AvatarFemaleRound", 300);
			_avatarsChild       = await LoadAvatarType("ParkAI/AvatarChild", 200);*/
			_avatarsMale        = await LoadAvatarType(GameAssets.Live.Peep_adult_male, 300);
			_avatarsFemale      = await LoadAvatarType(GameAssets.Live.Peep_adult_female, 300);
			_avatarsMaleRound   = await LoadAvatarType(GameAssets.Live.Peep_adult_male_round, 300);
			_avatarsFemaleRound = await LoadAvatarType(GameAssets.Live.Peep_adult_female_round, 300);
			_avatarsChild       = await LoadAvatarType(GameAssets.Live.Peep_child, 200);


			_ready = true;

			/*async UniTask<ComponentPool<ParkAIAvatar>> LoadAvatarType(string prefabAddress, int amount)
			{
				AsyncOperationHandle<GameObject> p      = await Addressables2.LoadHandleAsync<GameObject>(prefabAddress);
				ParkAIAvatar                     avatar = p.Result.GetComponent<ParkAIAvatar>();
				var                              pool   = new ComponentPool<ParkAIAvatar>(root.AddChild(prefabAddress).transform) {safetyChecks = false, prefab = avatar, allocateTemp = true};
				pool.AllocateAdd(amount);
				return pool;
			}*/

			async UniTask<ComponentPool<ParkAIAvatar>> LoadAvatarType(ParkAIAvatar prefab, int amount)
			{
				var pool = new ComponentPool<ParkAIAvatar>(root.AddChild(prefab.name).transform) { safetyChecks = false, prefab = prefab, allocateTemp = true};
				pool.AllocateAdd(amount);
				return pool;
			}
		}

		/*private async UniTaskVoid Start()
		{
			//UniTask.DelayFrame(2);

			//if (!GameOptions.current.ow_guests) return;

			//Debug.Log($"Spawn Agent Avatars ({AvatarNumber}): {sw.ElapsedTicks}/{sw.ElapsedMilliseconds}");
		}*/

		private void OnDestroy() => AvoidanceSystem?.Dispose();

		public static void OnAddSceneRegionObject(SceneRegionObjectBase obj)
		{
			DebugLogger.Log("Add scene region object " + obj, LogContext.Pathfinding, LogPriority.Low);
		}

		public static void OnRemoveSceneRegionObject(SceneRegionObjectBase obj)
		{
			DebugLogger.Log("Remove scene region object " + obj);
		}

		public async UniTask Init(LevelManifest manifest, PeepFilter? filter = null)
		{
			if (!manifest.ParkAIEnabled) return;
			if (!GameOptions.current.ow_guests) return;

			if (manifest.ParkAIGraphs.Count <= 0)
			{
				DebugLogger.Log($"No ParkAIGraphs set up for manifest {manifest.name}. ParkAI will not start running.", LogContext.Pathfinding, LogPriority.High);
				return;
			}

			if (manifest.PeepCount <= 0)
			{
				DebugLogger.Log($"Peep count for {manifest.name} is zero. ParkAI will not start running.", LogContext.Pathfinding, LogPriority.High);
				return;
			}

			if (await Init(manifest.ParkAIGraphs, manifest.PeepCount))
				_sourceManifest = manifest;
		}

		public async UniTask<bool> Init(List<RegionGraphAsset> graphs, int peepCount, PeepFilter? filter = null)
		{
			if (State != SystemState.Inactive) return false;

			// Gen graph
			_loadedGraph = new RuntimeParkAIGraph(this);
			foreach (var asset in graphs)
			{
				if (asset)
				{
					_loadedGraph.AddGraph(asset.Graph);
				}
			}

			_loadedGraph.LinkPortals();
			_loadedGraph.AddRuntimeObjects();

			// Gen peeps
			for (int i = 0; i < peepCount; i++)
			{
				(IndexedSpritesheetAsset bodySheet, IndexedSpritesheetAsset headSheet, PeepDef definition) = PeepGenerator.MakePeep(filter ?? default);

				ParkAIPeep peep = new ParkAIPeep();
				peep.Definition = definition;

				peep.Stats                 = PeepStats.RandomStats(0.2f, 0.6f);
				peep.Stats.Tiredness.value = UnityEngine.Random.Range(0, peep.Stats.Tiredness.cap);

				ComponentPool<ParkAIAvatar> pool = null;

				if (definition.Type == PeepType.Child)
				{
					pool = _avatarsChild;
				}
				else
				{
					switch (definition.Gender)
					{
						case PeepGender.Male:
							if (definition.BodyType == PeepBodyType.Average) pool    = _avatarsMale;
							else if (definition.BodyType == PeepBodyType.Round) pool = _avatarsMaleRound;
							break;

						case PeepGender.Female:
							if (definition.BodyType == PeepBodyType.Average) pool    = _avatarsFemale;
							else if (definition.BodyType == PeepBodyType.Round) pool = _avatarsFemaleRound;
							break;
					}
				}

				if (pool != null)
				{
					ParkAIAvatar avatar = pool.Rent();

					peep.Avatar     = avatar;
					peep.HasAvatar  = true;
					peep.SourcePool = pool;

					_billboardManager.Add(avatar.BillboardTransform);

					ColorReplacementProfile skinProfile = GameAssets.Live.PeepSpriteDatabase.SkinProfiles.RandomElement(_rand);
					ColorReplacementProfile hairProfile = GameAssets.Live.PeepSpriteDatabase.HairProfiles.RandomElement(_rand);

					float headOffset = 0;
					if (definition.HeadAccessory != PeepAccessory.None)
					{
						if (definition.Type == PeepType.Child)
						{
							headOffset = 0.06f;
						}
						else if (definition.BodyType == PeepBodyType.Round)
						{
							headOffset = .2f;
						}
						else
						{
							headOffset = .125f;
						}
					}


					avatar.HeadRenderer.transform.localPosition = new Vector3(0, avatar.originalHeadLocalPos.y + headOffset, -0.01f);

					avatar.HeadSetter.Profiles.Clear();
					avatar.BodySetter.Profiles.Clear();

					avatar.HeadSetter.Profiles.Add(skinProfile);
					avatar.HeadSetter.Profiles.Add(hairProfile);

					avatar.BodySetter.Profiles.Add(skinProfile);

					peep.HeadFrames = LoadSheet(headSheet.spritesheet);
					peep.BodyFrames = LoadSheet(bodySheet.spritesheet);

					// TODO: animation
				}

				//Choose a path for a peep:
				{
					Graph spawn_graph = _loadedGraph.Graphs[0];

					Node      spawn_node = null;
					GraphPath graph_path = default;

					if (spawn_graph.PortalPaths.Count > 0)
					{
						int    path_ind = _rand.Next(0, spawn_graph.PortalPaths.Count);
						Node[] path     = spawn_graph.PortalPaths[path_ind];

						Node  node;
						float total_area = 0;
						for (int j = 0; j < path.Length; j++)
						{
							node = path[j];
							if (node.region_object is RegionShape2D shape)
								total_area += shape.GetArea();
						}

						_scratchWeights.Clear();

						for (int j = 0; j < path.Length; j++)
						{
							node = path[j];
							if (node.region_object is RegionShape2D shape)
								_scratchWeights.Add(shape.GetArea() / total_area);
						}

						float r = _rand.NextFloat();
						int   ind;
						for (ind = 0; ind < path.Length - 1; ind += 1)
						{
							r -= _scratchWeights[ind];
							if (r <= 0 /*&& path[ind].enabled*/) break;
						}

						//int ind         = _rand.Next(1, path.Length - 1);

						spawn_node = path[ind];

						graph_path = new GraphPath
						{
							array = path,
							index = ind
						};
					}
					else
					{
						spawn_node = spawn_graph.AllShapes.RandomElement(_rand);
					}

					peep.Agent = new PeepAgent((ushort)i, peep, new GraphLocation(spawn_node.graph, spawn_node), _loadedGraph);

					if (spawn_graph.PortalPaths.Count > 0)
					{
						peep.Agent.SetGraphPath(graph_path);
					}
				}

				// Get an avatar for the peep


				AllPeeps.Add(peep);
			}

			UniTask.DelayFrame(1);

			for (int i = 0; i < AllPeeps.Count; i++)
			{
				if (AllPeeps[i].HasAvatar)
				{
					AllPeeps[i].Avatar.HeadSetter.UpdateMaterialProperties();
					AllPeeps[i].Avatar.BodySetter.UpdateMaterialProperties();
					/*AllPeeps[i].Avatar.HeadRenderer.gameObject.SetActive(false);
					AllPeeps[i].Avatar.BodyRenderer.gameObject.SetActive(false);*/
				}
			}

			/*UniTask.DelayFrame(2);

			for (int i = 0; i < AllPeeps.Count; i++) {
				if (AllPeeps[i].HasAvatar) {
					Sprite spr = AllPeeps[i].Avatar.HeadRenderer.sprite;
					AllPeeps[i].Avatar.HeadRenderer.sprite = null;
					AllPeeps[i].Avatar.HeadRenderer.sprite = spr;
				}
			}*/

			RebuildBuckets();

			State = SystemState.Running;

			LayerController.ManuallyUpdateActivation();

			return true;
		}

		public void Deinit()
		{
			if (State == SystemState.Inactive) return;

			// Dump Graph
			_loadedGraph = null;

			// Return all avatars
			ParkAIPeep peep;
			for (int i = 0; i < AllPeeps.Count; i++)
			{
				peep = AllPeeps[i];
				if (peep.HasAvatar)
				{
					_billboardManager.Remove(peep.Avatar.BillboardTransform);
					peep.SourcePool.ReturnSafe(peep.Avatar);
				}

				peep.ClearEmote();
			}

			// Dump Peeps
			AllPeeps.Clear();
			AvoidanceSystem.Reset();

			// YEET
			ResetBuckets();

			State = SystemState.Inactive;

			_sourceManifest = null;
		}

		void ResetBuckets()
		{
			/*_nearBuckets.Reset();
			_farBuckets.Reset();
			_aiBuckets.Reset();*/

			_buckets.Clear();
		}

		void RebuildBuckets()
		{
			int bucket   = 0;
			int inBucket = 0;
			_bucketIndex = 0;

			ResetBuckets();
			_buckets.Add(new Bucket(0));

			for (int i = 0; i < AllPeeps.Count; i++)
			{
				AllPeeps[i].Bucket = _bucketIndex;
				inBucket++;

				if (inBucket >= Config.BucketSize && i != AllPeeps.Count - 1)
				{
					_bucketIndex++;
					inBucket = 0;
					_buckets.Add(new Bucket(_bucketIndex));
				}
			}
		}

		[LuaGlobalFunc("parkai_resume")]
		public static void Resume()
		{
			if (Live.State != SystemState.Suspended) return;
			Live.State = SystemState.Running;

			for (int i = 0; i < Live.AllPeeps.Count; i++)
			{
				ParkAIPeep p = Live.AllPeeps[i];
				p.Avatar.Show();
			}
		}

		[LuaGlobalFunc("parkai_suspend")]
		public static void Suspend()
		{
			if (Live.State == SystemState.Suspended) return;
			Live.State = SystemState.Suspended;

			for (int i = 0; i < Live.AllPeeps.Count; i++)
			{
				ParkAIPeep p = Live.AllPeeps[i];
				p.Avatar.Hide();
				p.ClearEmote();
			}
		}

		public void OnLevelLoad(LevelManifest manifest) => Init(manifest);
		public void OnLevelExit()                       => Deinit();

		void Update()
		{
			bool playerExists = ActorController.playerActor != null;

#if PARKAI_DEBUG
			_debugInfo.Reset();
#endif

			if (State >= SystemState.Running && !GameController.IsWorldPaused)
			{
				if (State == SystemState.Running)
				{
					bool  buckets       = _buckets.Count > 0;
					float bucketDT      = Time.deltaTime;
					int   CurrentBucket = 0;
					if (buckets)
					{
						Profiler.BeginSample("Buckets");
						Bucket bucket = _buckets.WrapGet(_bucketIndex);
						CurrentBucket = bucket.index;
						_bucketIndex++;
						bucket.DebugVisTicket = Bucket.DEBUG_VIS_TIME;
						if (bucket.lastUpdateTime == -1)
							bucket.lastUpdateTime = Time.time;

						bucketDT              = Mathf.Clamp(Time.time - bucket.lastUpdateTime, 0, 1);
						bucket.lastUpdateTime = Time.time;
						Profiler.EndSample();
					}

					if (GameController.DebugMode)
					{
						for (int i = 0; i < _buckets.Count; i++)
						{
							_buckets[i].DebugVisTicket -= 1;
							_buckets[i].DebugVisTicket =  Mathf.Max(0, _buckets[i].DebugVisTicket);
						}
					}

					float dt = Time.deltaTime;

					if (_loadedGraph != null)
					{
						Profiler.BeginSample("Graph Brains");
						for (int i = 0; i < _loadedGraph.Brains.Count; i++)
						{
							_loadedGraph.Brains[i].Update(dt);
						}

						Profiler.EndSample();
					}

					Profiler.BeginSample("LOD");
					{
						Vector3 refPoint = Vector3.zero;
						if (playerExists && Config.LODReferencePoint == PeepLODReferencePoint.Player)
						{
							refPoint = ActorController.PlayerPosition.Value;
						}
						else
						{
							refPoint = GameCams.Live.UnityCam.transform.position;
						}

						Vector3    peepPosition;
						float      distance;
						ParkAIPeep peep;
						//PeepLOD    lod;
						for (int i = 0; i < AllPeeps.Count; i++)
						{
							peep         = AllPeeps[i];
							peepPosition = peep.Agent.Position;
							distance     = Vector3.Distance(peepPosition, refPoint);

							if (distance > Config.LOD_Dist_4) peep.LOD      = PeepLOD.LOD4;
							else if (distance > Config.LOD_Dist_3) peep.LOD = PeepLOD.LOD3;
							else if (distance > Config.LOD_Dist_2) peep.LOD = PeepLOD.LOD2;
							else if (distance > Config.LOD_Dist_1) peep.LOD = PeepLOD.LOD1;
							else peep.LOD                                   = PeepLOD.LOD0;

							peep.LODDistance = distance;

#if PARKAI_DEBUG
							_debugInfo.LOD_numbers[peep.LOD] += 1;
#endif
						}
					}
					Profiler.EndSample();

					Profiler.BeginSample("Agents");
					for (int i = 0; i < AllPeeps.Count; i++)
					{
						Profiler.BeginSample("Agent Update");

						var peep = AllPeeps[i];

						bool periodicUpdate = !buckets || CurrentBucket == peep.Bucket || !Config.UseBuckets || peep.Bucket < 0;

						float distance = float.MaxValue;
						if (ActorController.PlayerPosition.HasValue)
						{
							//peep.LOD = PeepLOD.Near;
							//distance = Vector3.Distance(peep.Agent.Position, ActorController.PlayerPosition.Value);
							//UpdateLOD(peep, distance);
						}

						peep.Agent.PreUpdate();

						if (periodicUpdate)
						{
							Profiler.BeginSample($"Periodic Update");
							peep.Agent.TryDecisionUpdate(_rand);
							peep.Agent.ActionTick(bucketDT, SimLevel.InMap, peep.LOD);
							peep.Agent.PeepTick(bucketDT);
							Profiler.EndSample();
						}

						peep.Agent.PreMovementUpdate(Time.deltaTime);

						// Do avoidance
						Profiler.BeginSample("Peep Avoidance");
						if (Config.UseAvoidance)
						{
							// Update Avoidance
							if (playerExists && !peep.UsingAvoidance)
							{
								if (peep.LODDistance <= Config.AvoidanceRadius || Config.GlobalAvoidance)
								{
									var (ID, ok) = AvoidanceSystem.AddAgent(peep.Agent.Position, peep.Agent.Velocity, 0.3f);
									if (ok)
									{
										peep.UsingAvoidance = true;
										peep.AvoidanceID    = ID;
									}
								}
							}
							else if (peep.UsingAvoidance)
							{
								if (distance >= Config.AvoidanceRadius && !Config.GlobalAvoidance || !playerExists)
								{
									AvoidanceSystem.RemoveAgent(peep.AvoidanceID);
									peep.UsingAvoidance = false;
								}
								else if (peep.UsingAvoidance)
								{
									AvoidanceSystem.UpdateAgent(peep.AvoidanceID, peep.Agent.Position, peep.Agent.Velocity);
								}
							}
						}

						Profiler.EndSample();

						Profiler.EndSample();
					}

					Profiler.EndSample();
				}

				if (Config.UseAvoidance && State == SystemState.Running)
				{
					AvoidanceSystem.StaticAgents.Clear();
					AvoidanceSystem.Obstacles.Clear();

					if (ActorController.Exists)
					{
						for (int i = 0; i < ActorController.partyActors.Count; i++)
						{
							Actor actor     = ActorController.partyActors[i];
							Actor character = actor;
							if (character == null) continue;

							AvoidanceSystem.StaticAgents.Add(new BurstAvoidanceSystem.StaticAgent
							{
								Position = character.Position,
								Radius   = 0.5f,
								Velocity = character.velocity
							});
						}
					}

					// TEMP
					foreach (var collection in AvoidanceObstacleCollection.All)
					{
						foreach (var obstacle in collection.Obstacles)
						{
							if (obstacle.Type == ObstacleType.Line)
							{
								for (int i = 0; i < obstacle.LineSegments; i++)
								{
									if (obstacle.GetSegmentWorld(i, out var p1, out var p2, collection.transform))
									{
										AvoidanceSystem.AddObstacle(p1.xz(), p2.xz(), obstacle.GetLineNormal(p1.xz(), p2.xz()));
									}
								}
							}
						}
					}

					AvoidanceSystem.Simulate(Time.deltaTime);
				}

				if (GameController.DebugMode && Config.Debug_DrawAvoidanceInfo)
					AvoidanceSystem.DebugDraw();

				if (State == SystemState.Running)
				{
					Profiler.BeginSample("Post Movement Update");
					for (int i = 0; i < AllPeeps.Count; i++)
					{
						var peep = AllPeeps[i];
						peep.Agent.PostMovementUpdate(Time.deltaTime, SimLevel.InMap, peep.LOD, _rand);
					}

					Profiler.EndSample();
				}

				Profiler.BeginSample("Avatars");


				Profiler.BeginSample("Visibility");
				if (State == SystemState.Running)
				{
					GeometryUtility.CalculateFrustumPlanes(GameCams.Live.UnityCam, _cam_frustum_planes);
					ParkAIPeep peep;
					bool       prevVisible;
					for (int i = 0; i < AllPeeps.Count; i++)
					{
						peep        = AllPeeps[i];
						prevVisible = peep.Visible;

						if (peep.LOD >= Config.LOD_Visible)
						{
							peep.Visible = false;
						}
						else
						{
							peep.Visible = GeometryUtility.TestPlanesAABB(_cam_frustum_planes, new Bounds(AllPeeps[i].Agent.Position, new Vector3(1, 2, 1)));
						}

						if (prevVisible != peep.Visible)
						{
							if (peep.Visible)
								peep.Avatar.Show();
							else
							{
								peep.Avatar.Hide();
								peep.ClearEmote();
							}
						}

#if PARKAI_DEBUG
						if (peep.Visible)
							_debugInfo.peeps_visible++;
#endif
					}
				}

				Profiler.EndSample();

				// "Activate" and "Deactivate" avatars based on visibility

				// Do updates for avatars that are active
				Profiler.BeginSample("Positioning");

				if (State == SystemState.Running)
				{
					ParkAIPeep peep;
					for (int i = 0; i < AllPeeps.Count; i++)
					{
						peep = AllPeeps[i];

						// TODO: Only do this if visible?
						if (peep.HasAvatar && peep.Visible)
						{
							peep.Avatar.transform.position = peep.Agent.Position;
						}

						//TODO: Figure out why we need to do this
						if (!peep.Fixed && peep.Visible)
						{
							peep.Avatar.BodyRenderer.gameObject.SetActive(false);
							peep.Avatar.HeadRenderer.gameObject.SetActive(false);

							peep.Avatar.BodyRenderer.gameObject.SetActive(true);
							peep.Avatar.HeadRenderer.gameObject.SetActive(true);

							peep.Fixed = true;
						}
					}
				}

				Profiler.EndSample();

				Profiler.BeginSample("Animation");

				if (State == SystemState.Running)
				{
					ParkAIPeep peep;
					float      dt = Time.deltaTime;

					int prevFrame;

					for (int i = 0; i < AllPeeps.Count; i++)
					{
						peep = AllPeeps[i];

						if (peep.HasAvatar && peep.Visible && peep.LOD <= Config.LOD_Animating)
						{
							// Find the current animation

							float      blending = MathUtil.ToWorldAzimuthBlendable(peep.LookDirection);
							Direction8 ordinal  = MathUtil.ToWorldAzimuthOrdinal(blending);

							AnimationSet headFrames = peep.HeadFrames;
							AnimationSet bodyFrames = peep.BodyFrames;

							AnimationBinding currentHead = AnimationBinding.Invalid;
							AnimationBinding currentBody = AnimationBinding.Invalid;

							int index = ((int)ordinal) - 1;

							switch (peep.Animation)
							{
								case ParkAIAnim.Walk:
									currentHead = headFrames.walk[index];
									currentBody = bodyFrames.walk[index];
									break;

								case ParkAIAnim.Sit:
									currentHead = headFrames.sit[index];
									currentBody = bodyFrames.sit[index];
									break;

								// Default is standing
								default:
									currentHead = headFrames.stand[index];
									currentBody = bodyFrames.stand[index];
									break;
							}

							FrameBinding headBinding;
							FrameBinding bodyBinding;

							prevFrame = peep.FrameIndex;

							if (peep.Animation != ParkAIAnim.Stand)
							{
								if (currentHead.frames.Length != 0)
								{
									headBinding = currentHead.frames[peep.FrameIndex % currentHead.frames.Length];
									//bodyBinding = currentBody.frames[peep.FrameIndex];

									// We use the head binding for the duration, since we assume the animations are the same length
									// We also

									peep.FrameTimer += dt * AVATAR_ANIMATION_SPEED;
									if (peep.FrameTimer >= headBinding.durationInSeconds)
									{
										peep.FrameIndex++;
										peep.FrameTimer = 0;
										if (peep.FrameIndex >= currentHead.Length)
											peep.FrameIndex = 0;
									}
								}
							}

							if (prevFrame != peep.FrameIndex || ordinal != peep.FrameDir)
							{
								var hFrames = currentHead.frames;
								var bFrames = currentBody.frames;
								if (peep.FrameIndex < hFrames.Length)
								{
									headBinding = hFrames[peep.FrameIndex];
									peep.Avatar.HeadRenderer.sprite = headBinding.Sprite;
								}

								if (peep.FrameIndex < bFrames.Length)
								{
									bodyBinding = bFrames[peep.FrameIndex];
									peep.Avatar.BodyRenderer.sprite = bodyBinding.Sprite;
								}

								peep.FrameDir = ordinal;
							}

						#if PARKAI_DEBUG
							_debugInfo.peeps_animating++;
						#endif

						}
					}
				}

				Profiler.EndSample();

				Profiler.BeginSample("Ground Snapping");
				if (Config.AvatarGroundSnapping)
				{
					ParkAIPeep   peep;
					ParkAIAvatar avatar;
					float        ground_y_offset;
					for (int i = 0; i < AllPeeps.Count; i++)
					{
						peep   = AllPeeps[i];
						avatar = peep.Avatar;
						if (peep.Visible && peep.LOD <= Config.LOD_GroundSnapping)
						{
							// TODO: This had an issue where if an agent paused at a path point, their snapping would turn off, and they would
							//		 visually display in the ground or in the air. We need to have a way to require ground snap updates.

							if (peep.Animation == ParkAIAnim.Sit) {
								ground_y_offset = avatar.sitting_y_offset;
							} else if (/*peep.Agent.MovementMode != PeepAgent.MoveMode.None &&*/ Physics.Raycast(avatar.transform.position + Vector3.up * 2f, Vector3.down, out var ray, 4f, Layers.Walkable.mask, QueryTriggerInteraction.Ignore)) {
								ground_y_offset = (ray.point.y - avatar.transform.position.y) + avatar.starting_y_offset;

								#if PARKAI_DEBUG
								_debugInfo.peeps_ground_snapping++;
								#endif
							}
							else ground_y_offset = 0;

							avatar.GroundPivotTransform.localPosition = new Vector3(0, ground_y_offset, 0);
						}
					}
				}

				Profiler.EndSample();

				#region Emotes

				Profiler.BeginSample("Emotes");
				if (Config.EnableEmotes)
				{
					//	IF we are not showing the maximum number of emotes already:
					//		Loop through every agent:
					//			IF agent has an active request,
					//			AND agent is not currently showing an emote,
					//			AND agent is within LOD range,
					//				Spawn an emote

					ParkAIPeep peep;

					for (int i = 0; i < AllPeeps.Count; i++)
					{
						peep = AllPeeps[i];
						if (peep.Visible && peep.LOD <= Config.LOD_Emotes)
						{
							if (peep.Agent.ShowingEmote)
							{
								if (peep.Emote == null || peep.Emote.state == EmotePopup.State.Off)
								{
									peep.Agent.ShowingEmote = false;
									peep.ClearEmote();
								}
#if PARKAI_DEBUG
								_debugInfo.emotes_showing++;
#endif
							}
							else if (peep.Agent.EmoteRequest.active && GameHUD.Live.emotePopupPool.ActiveCount < 30)
							{
								peep.Agent.ShowingEmote        = true;
								peep.Agent.EmoteRequest.active = false;
								EmotePopup emote = GameHUD.Live.emotePopupPool.Rent();

								peep.Emote = emote;
								emote.SetEmote(peep.Agent.EmoteRequest.emote);
								emote.state      = EmotePopup.State.On;
								emote.AutoReturn = false;
								emote.Element.SetPositionModeWorldPoint(new WorldPoint(peep.Avatar.BillboardTransform.gameObject, new Vector3(0, 1.6f, 0), true), Vector3.zero);
								emote.UpdateVisibility();
							}
						}
						else
						{
							peep.ClearEmote();
						}
					}
				}

				Profiler.EndSample();
				#endregion


				Profiler.EndSample();

			}

			if (GameController.DebugMode)
			{
				if (Config.Debug_DrawAgentIDs)
				{
					CommandBuilder ingame = Draw.ingame;
					for (int i = 0; i < AllPeeps.Count; i++)
					{
						ParkAIPeep peep = AllPeeps[i];

						if (peep.AvatarOld != null)
						{
							ingame.Label2D(peep.Agent.Position + Vector3.up * 2, $"{peep.Agent.ID.ToString()}", 16f, LabelAlignment.Center);
						}
					}
				}

				if (_loadedGraph != null)
				{
					for (int i = 0; i < _loadedGraph.Nodes.Count; i++)
					{
						Node node = _loadedGraph.Nodes[i];

						if (node.type == NodeType.Stall && node.spatial != null)
						{
							for (int j = 0; j < node.queue_slots.Length; j++)
							{
								Slot slot = node.queue_slots[j];
								Draw.ingame.Circle(slot.world_pos, Vector3.up, ParkAIQueue.SLOT_RADIUS, slot.occupied ? ColorsXNA.IndianRed : Color.white);
							}
						}
					}
				}
			}
		}

		private static void PreLateUpdate()
		{
			if (!Application.isPlaying || !Exists) return;
			Profiler.BeginSample("Billboard Jobs");
			Live._billboardManager.Update();
			Profiler.EndSample();
		}

		// TODO:
		public struct LODCalculationJob : IJobParallelForTransform
		{
			public void Execute(int index, TransformAccess transform) { }
		}

		// Animation
		//==========================================================================================================================

	#region Animation

		// NOTE: Not used for actual animations, just for loading animations from the spritesheets
		private static SpritesheetIndexing _indexing;

		public AnimationSet LoadSheet([NotNull] IndexedSpritesheet source)
		{
			if (_indexing == null) _indexing = new SpritesheetIndexing();

			_indexing.Clear();
			_indexing.AddSource(source);

			AnimationSet set = new AnimationSet
			{
				stand = GetOrdinalEightDir(AnimID.Stand),
				walk  = GetOrdinalEightDir(AnimID.Walk),
				sit   = GetOrdinalEightDir(AnimID.Sit),
			};

			return set;

			AnimationBinding[] GetOrdinalEightDir(AnimID anim)
			{
				AnimationBinding[] arr = new AnimationBinding[8];

				for (int j = 0; j < DirUtil.Directions.Length; j++) {
					var dir = DirUtil.Directions[j];
					if (_indexing.GetAnimation(anim, dir, out AnimationBinding binding))
						arr[j] = binding;
					else {
						DebugLogger.Log($"ParkAIController: Failed to get binding for indexing {anim}, {dir}, {_indexing}", LogContext.Pathfinding, LogPriority.High);
					}
				}

				return arr;
			}
		}

	#endregion

		//==========================================================================================================================


#if UNITY_EDITOR
		void OnDrawGizmos()
		{
			var offset = Vector3.right * 0.25f;

			if (AllPeeps != null)
			{
				for (int i = 0; i < AllPeeps.Count; i++)
				{
					var peep = AllPeeps[i];

					/*switch (peep.LOD) {
						case PeepLOD.Near:
							Gizmos.color = Color.red;
							break;
						case PeepLOD.Far:
							Gizmos.color = Color.blue;
							break;
					}
*/

					/*var pos = peep.Agent.GetTargetWorldPos();
					Gizmos.DrawWireSphere(pos, 0.3f);*/

					/*var path = peep.Agent.requestedPath;

					if (peep.Agent.CurrentAction.type == ParkAIAgent.ActionType.WalkTo) {

						if (path != null && path.IsDone()) {
							Draw.Polyline(path.vectorPath, Color.yellow);
						} else {
							Debug.DrawLine(peep.Agent.CurrentAction.move.start + offset, peep.Agent.CurrentAction.move.destination + offset, Color.cyan, 0.1f, false);
						}
					}*/


					/*if (DrawNumberGizmos)
						Handles.Label(pos.Value + Vector3.up * 1.5f, i.ToString(), EventStyles.GetTitleWithColor(Color.magenta));*/
				}
			}

			/*if (RuntimeGraph != null) {
				for (int i = 0; i < RuntimeGraph.PortalPaths.Count; i++) {
					//Color c = UnityEngine.Random.ColorHSV(0, 1, 0.8f, 0.8f, 0.8f, 0.8f);

					for (int j = 0; j < RuntimeGraph.PortalPaths[i].Length; j++) {
						var obj = RuntimeGraph.PortalPaths[i][j];

						GUI.color = Color.red;
						Handles.Label(obj.Spatial.Transform.Position + Vector3.up + (Vector3.up * 2 * i), j.ToString());
					}

					GUI.color = Color.white;
				}
			}*/
		}
#endif


		[DebugRegisterGlobals]
		public static void RegisterMenu()
		{
			DebugSystem.RegisterMenu("ParkAI");
		}

		private int peepSelected = 0;

		private int        gui_init_peepcount = 0;
		private PeepFilter? debugFilter;

		public void OnLayout(ref DebugSystem.State state)
		{
			if (state.Begin("ParkAI"))
			{
				switch (State)
				{
					case SystemState.Inactive:

						g.Text("Init: ");

						if (g.Button("Manifest"))
						{
							if (GameController.ActiveLevel && GameController.ActiveLevel.Manifest)
							{
								Init(GameController.ActiveLevel.Manifest, debugFilter).ForgetWithErrors();
							}
						}


						if (g.Button("Custom Peep Count"))
						{
							if (GameController.ActiveLevel && GameController.ActiveLevel.Manifest)
							{
								Init(GameController.ActiveLevel.Manifest.ParkAIGraphs, gui_init_peepcount, debugFilter).ForgetWithErrors();
							}
						}

						g.SameLine();
						g.InputInt("##Custom Peep Count", ref gui_init_peepcount);
						gui_init_peepcount = Mathf.Max(0, gui_init_peepcount);


						if (g.CollapsingHeader("Peep Filter")) {
							if (!debugFilter.HasValue) {
								if (g.Button("Init"))
									debugFilter = new PeepFilter();
							} else {
								if (g.Button("Set to null"))
									debugFilter = null;

								if (debugFilter != null) {
									PeepFilter filter = debugFilter.Value;

									g.Text("Type: ");
									g.PushID("_type");
									if (filter.Type == null && g.Button("Initialize"))
										filter.Type = default(PeepType);
									else if(filter.Type.HasValue) {
										var _temp = filter.Type.Value;
										AImgui.EnumDrawer("", ref _temp);
										filter.Type = _temp;

										if(g.Button("Set to null"))
											filter.Type = null;
									}
									g.PopID();

									g.Text("Gender: ");
									g.PushID("_Gender:");
									if (filter.Gender == null && g.Button("Initialize"))
										filter.Gender = default(PeepGender);
									else if(filter.Gender.HasValue) {
										var _temp = filter.Gender.Value;
										AImgui.EnumDrawer("", ref _temp);
										filter.Gender = _temp;

										if(g.Button("Set to null"))
											filter.Gender = null;
									}
									g.PopID();

									g.Text("BodyType: ");
									g.PushID("_BodyType:");
									if (filter.BodyType == null && g.Button("Initialize"))
										filter.BodyType = default(PeepBodyType);
									else if(filter.BodyType.HasValue) {
										var _temp = filter.BodyType.Value;
										AImgui.EnumDrawer("", ref _temp);
										filter.BodyType = _temp;

										if(g.Button("Set to null"))
											filter.BodyType = null;
									}
									g.PopID();

									g.Text("Race: ");
									g.PushID("Race:_");
									if (filter.Race == null && g.Button("Initialize"))
										filter.Race = default(PeepRace);
									else if(filter.Race.HasValue) {
										var _temp = filter.Race.Value;
										AImgui.EnumDrawer("", ref _temp);
										filter.Race = _temp;

										if(g.Button("Set to null"))
											filter.Race = null;
									}
									g.PopID();

									g.Text("Head Accessory: ");
									g.PushID("_Head Accessory");
									if (filter.HeadAccessory == null && g.Button("Initialize"))
										filter.HeadAccessory = default(PeepAccessory);
									else if(filter.HeadAccessory.HasValue) {
										var _temp = filter.HeadAccessory.Value;
										AImgui.EnumDrawer("", ref _temp);
										filter.HeadAccessory = _temp;

										if(g.Button("Set to null"))
											filter.HeadAccessory = null;
									}
									g.PopID();

									debugFilter = filter;
								}
							}
						}

						break;

					case SystemState.Paused:
					case SystemState.Running:
						g.Text("Manifest: " + (_sourceManifest ? _sourceManifest.name : "null"));

						if (g.Button("Deinit"))
						{
							Deinit();
						}

						if (g.Button("Suspend"))
						{
							Suspend();
						}

						if (g.Button("Total Redistribution"))
						{
							PerformTotalPeepRedistribution();
						}

#if PARKAI_DEBUG
						AImgui.Text("Stats:", ColorsXNA.MediumSlateBlue);
						AImgui.Text($"{AllPeeps.Count} peeps | {_debugInfo.peeps_visible} vis | {_debugInfo.peeps_animating} anim | {_debugInfo.peeps_ground_snapping} snap | {_debugInfo.emotes_showing} shadow | {_debugInfo.emotes_showing} emotes",
							ColorsXNA.Orchid);
#endif

						break;

					case SystemState.Suspended:
						if (g.Button("Resume"))
						{
							Resume();
						}

						break;
				}

				if (g.BeginTabBar("tabs"))
				{
					if (g.BeginTabItem("Main"))
					{
						if (g.CollapsingHeader("Config"))
						{
							g.Text("Main:");
							g.InputFloat("Simulation Speed", ref Config.SimSpeed);
							g.Checkbox("Use Pathfinding", ref Config.UsePathfinding);
							g.InputInt("RNG Seed", ref Config.RNGSeed);
							g.InputFloat("Walk Speed Modifier", ref Config.Peep_WalkSpeedMod);
							g.InputFloat("Stat Gain Mod", ref Config.Peep_StatGainMod);
							g.Checkbox("Draw Agent IDs", ref Config.Debug_DrawAgentIDs);
						}

						if (g.CollapsingHeader("Avatars"))
						{
							g.Checkbox("Enable Avatars", ref Config.Avatars);
							g.Checkbox("Animation", ref Config.AvatarAnimation);
							g.Checkbox("Ground Snapping", ref Config.AvatarGroundSnapping);
						}

						if (g.CollapsingHeader("Buckets"))
						{
							g.Checkbox("Use Buckets", ref Config.UseBuckets);
							g.InputInt("Bucket Size", ref Config.BucketSize);
							if (g.Button("Rebuild Buckets"))
							{
								RebuildBuckets();
							}

							for (int i = 0; i < _buckets.Count; i++)
							{
								Bucket b   = _buckets[i];
								Color  col = Color.Lerp(ColorsXNA.Goldenrod, Color.magenta, b.DebugVisTicket / Bucket.DEBUG_VIS_TIME);
								g.TextColored(col.ToV4(), $"{i}");
							}
						}

						if (g.CollapsingHeader("Graph")) {
							if (Live._loadedGraph != null) {
								Live._loadedGraph.DrawImgui();
							}
						}

						if (g.CollapsingHeader("LOD"))
						{
							AImgui.EnumDrawer("LOD reference point", ref Config.LODReferencePoint);
							g.Separator();
							AImgui.EnumDrawer("Peeps Visible", ref Config.LOD_Visible);
							AImgui.EnumDrawer("Peeps Animating", ref Config.LOD_Animating);
							AImgui.EnumDrawer("Peeps Ground Snapping", ref Config.LOD_GroundSnapping);
							g.Separator();
							AImgui.Text("LOD Distances:", ColorsXNA.Green);
							g.Indent(16);
							g.InputFloat("LOD0", ref Config.LOD_Dist_0);
							g.InputFloat("LOD1", ref Config.LOD_Dist_1);
							g.InputFloat("LOD2", ref Config.LOD_Dist_2);
							g.InputFloat("LOD3", ref Config.LOD_Dist_3);
							g.InputFloat("LOD4", ref Config.LOD_Dist_4);
							g.Unindent(16);

#if PARKAI_DEBUG
							AImgui.Text("LOD agent count:", ColorsXNA.Goldenrod);
							g.Indent(12);
							foreach (var key in _debugInfo.LOD_numbers.Keys)
							{
								AImgui.Text($"{key}: {_debugInfo.LOD_numbers[key]}", ColorsXNA.Goldenrod);
							}

							g.Unindent(12);

#else
							AnjinGui.Text("Not compiled with PARKAI_DEBUG defined, no realtime LOD info can be displayed.", ColorsXNA.OrangeRed);
#endif
						}

						if (g.CollapsingHeader("Avoidance"))
						{
							g.Checkbox("Enabled", ref Config.UseAvoidance);
							g.Checkbox("Global", ref Config.GlobalAvoidance);
							g.Checkbox("Draw Debug Info", ref Config.Debug_DrawAvoidanceInfo);
							g.InputFloat("Active Radius", ref Config.AvoidanceRadius);
							g.Separator();
							AvoidanceSystem?.OnImgui();
						}


						if (g.CollapsingHeader("Emotes"))
						{
							g.Checkbox("Enabled", ref Config.EnableEmotes);
						}

						g.EndTabItem();
					}

					if (g.BeginTabItem("Peeps"))
					{
						if (AllPeeps != null && AllPeeps.Count > 0)
						{
							g.BeginChild("list", new Vector2(86, 0), true);
							for (int i = 0; i < AllPeeps.Count; i++)
							{
								if (g.Selectable("Peep " + i, peepSelected == i))
									peepSelected = i;
							}

							g.EndChild();

							g.SameLine();

							g.BeginGroup();
							if (peepSelected >= 0 && peepSelected < AllPeeps.Count)
							{
								ParkAIPeep p = AllPeeps[peepSelected];
								ImGuiPeepStats(p);
							}

							g.EndGroup();
						}

						g.EndTabItem();
					}

					g.EndTabBar();
				}
			}
		}

		public void ImGuiPeepStats(ParkAIPeep p)
		{
			g.Text("Peep:");
			g.Separator();

			g.Text("Type: " + p.Definition.Type);
			g.Text("Gender: " + p.Definition.Gender);
			g.Text("Bodytype: " + p.Definition.BodyType);
			g.Text("Race: " + p.Definition.Race);
			g.Text("HeadAccessory: " + p.Definition.HeadAccessory);
			g.Text("BodyAccessory: " + p.Definition.BodyAccessory);

			var draw = g.GetWindowDrawList();

			var pos      = g.GetCursorScreenPos();
			var base_pos = pos;

			float rowHeight = 24;

			void AddToWidth(float w, ref Vector2 _pos)
			{
				if (_pos.x + w > g.GetWindowWidth())
					_pos = new Vector2(0, _pos.y + rowHeight);
				else
					_pos.x = _pos.x += w;
			}

			void rowHeader(string header, float w, ref Vector2 _pos)
			{
				draw.AddText(new Vector2(_pos.x, _pos.y), Color.white.ToUint(), header);
				AddToWidth(w, ref _pos);
			}

			void rowText(string text, Color color, float w, ref Vector2 _pos)
			{
				//float text_w = g.CalcTextSize(text).x;
				draw.AddText(new Vector2(_pos.x /*+ (w - text_w)*/, _pos.y), color.ToUint(), text);
				AddToWidth(w, ref _pos);
			}

			float statBar(string label, ref Stat stat, ref Vector2 _pos)
			{
				var base_x = _pos.x;

				var sz = 76 /*g.CalcTextSize(label).x*/;
				var w  = g.GetWindowWidth();

				float threshold = stat.threshold / stat.cap;

				rowText(label, ColorsXNA.Violet, 76, ref _pos);

				rowText($"{stat.value:0.##}", ColorsXNA.Goldenrod, 38, ref _pos);
				rowText($"/{stat.cap:0.##} ", ColorsXNA.Goldenrod, 48, ref _pos);
				rowText($"{(stat.rate * 60):0.##} / min ", ColorsXNA.GreenYellow, 76, ref _pos);
				rowText($"{stat.threshold:0.##}", ColorsXNA.LimeGreen, 76, ref _pos);
				rowText($"{stat.Urgency:0.##}", ColorsXNA.PaleVioletRed, 64, ref _pos);

				/*draw.AddText(new Vector2(pos.x + sz, pos.y), 		ColorsXNA.Goldenrod.ToUint(), 	$"{stat.value:#.##}");
				draw.AddText(new Vector2(pos.x + sz + 38, pos.y), 	ColorsXNA.Goldenrod.ToUint(), 	$"/{stat.cap:#.##} ");
				draw.AddText(new Vector2(pos.x + sz + 86, pos.y), 	ColorsXNA.GreenYellow.ToUint(), $"{(stat.rate * 60):#.##}/min ");
				draw.AddText(new Vector2(pos.x + sz + 128, pos.y), 	ColorsXNA.PaleVioletRed.ToUint(), $"{(stat.Urgency):#.##}");*/

				Vector2 min = _pos;
				Vector2 max = _pos + new Vector2(w - 500, 16);

				Vector2 pMin(float percentage) => new Vector2(Mathf.Lerp(min.x, max.x, percentage), min.y);
				Vector2 pMax(float percentage) => new Vector2(Mathf.Lerp(min.x, max.x, percentage), max.y);

				draw.AddRectFilled(min, pMax(stat.value / stat.cap), ColorsXNA.Maroon.ToUint(), 4, ImDrawCornerFlags.All);
				draw.AddRect(min, max, ColorsXNA.Violet.ToUint(), 4, ImDrawCornerFlags.All, 1f);

				draw.AddLine(pMin(threshold), pMax(threshold), ColorsXNA.LimeGreen.ToUint(), 2);

				_pos.x =  base_x;
				_pos   += new Vector2(0, rowHeight);
				return rowHeight;
			}

			g.Dummy(new Vector2(0, 18));
			pos += new Vector2(0, 18);

			rowHeader("Stat:", 76, ref pos);
			rowHeader("Value/Cap:", 38 + 48, ref pos);
			rowHeader("Rate:", 76, ref pos);
			rowHeader("Threshold:", 76, ref pos);
			rowHeader("Urgency:", 64, ref pos);
			pos.x =  base_pos.x;
			pos   += new Vector2(0, 18);

			float height = 18;

			height += statBar("Hunger:", ref p.Stats.Hunger, ref pos);
			height += statBar("Thirst:", ref p.Stats.Thirst, ref pos);
			height += statBar("Boredom:", ref p.Stats.Boredom, ref pos);
			height += statBar("Tiredness:", ref p.Stats.Tiredness, ref pos);
			height += statBar("Bathroom:", ref p.Stats.Bathroom, ref pos);

			g.Dummy(new Vector2(0, height + 16));

			g.Text($"Highest Urgency: {p.Stats.Urgency_Highest}");

			g.Separator();

			g.Text("Agent:");
			g.Separator();
			g.Text("ID: " + p.Agent.ID);
			g.Text("State: " + p.Agent.state);
			g.Text("Position: " + p.Agent.Position);
			g.Text("Goal: " + p.Agent.Goal);
			g.Text("Graph Location: " + p.Agent.Location);

			g.Separator();
			g.Text("Emotes:");
			g.Text("Request: " + p.Agent.EmoteRequest);
			g.Text("Showing Emote: " + p.Agent.ShowingEmote);
			g.Text("Current Spawned Emote: " + (p.Emote ? p.Emote.ToString() : "null"));

			g.Separator();
			g.Text("Actions:");
			g.Indent(16);
			if (p.Agent.Actions.state != PeepAgent.ActionQueue.State.Idle)
			{
				g.Text("Current: " + p.Agent.Actions.current.type);
				for (int i = 0; i < p.Agent.Actions.count; i++)
				{
					var action = p.Agent.Actions.actions[i];
					g.Text(i + ": " + action.type);
				}
			}

			g.Unindent(16);
		}

		public struct TickTimer
		{
			[HideInEditorMode]
			public float time;
			[HideInEditorMode]
			public float delta;
			float tempDelta;

			public float rate;

			public TickTimer(float rate) : this() => this.rate = rate;

			public bool Tick(float dt)
			{
				time      -= dt;
				tempDelta += dt;

				if (time <= 0)
				{
					time      = 1.0f / rate;
					delta     = tempDelta;
					tempDelta = 0f;
					return true;
				}

				return false;
			}
		}

		class BucketGroup
		{
			public List<Bucket> Buckets = new List<Bucket>();
			public int          BucketSize;
			public int          BucketIndex;

			public BucketGroup(int bucketSize, int bucketIndex)
			{
				BucketSize  = bucketSize;
				BucketIndex = bucketIndex;
			}

			public bool Update(out int index, out float dt)
			{
				index = 0;
				dt    = 0;
				if (Buckets.Count <= 0) return false;

				Bucket bucket = Buckets.WrapGet(BucketIndex);
				index = bucket.index;
				BucketIndex++;

				bucket.DebugVisTicket = Bucket.DEBUG_VIS_TIME;
				if (bucket.lastUpdateTime == -1)
					bucket.lastUpdateTime = Time.time;

				dt                    = Mathf.Clamp(Time.time - bucket.lastUpdateTime, 0, 1);
				bucket.lastUpdateTime = Time.time;

				if (GameController.DebugMode)
				{
					for (int i = 0; i < Buckets.Count; i++)
					{
						Buckets[i].DebugVisTicket -= 1;
						Buckets[i].DebugVisTicket =  Mathf.Max(0, Buckets[i].DebugVisTicket);
					}
				}

				return true;
			}

			public void Reset()
			{
				Buckets.Clear();
				BucketIndex = 0;
			}
		}

		class Bucket
		{
			public const float DEBUG_VIS_TIME = 10;

			public int   index;
			public float DebugVisTicket;
			public float lastUpdateTime = -1;

			public Bucket(int index)
			{
				this.index     = index;
				DebugVisTicket = 0;
			}
		}

		public struct ParkAIDebugInfo
		{
			public int                      peeps_visible;
			public int                      peeps_animating;
			public int                      peeps_ground_snapping;
			public int                      shadows_visible;
			public int                      emotes_showing;
			public Dictionary<PeepLOD, int> LOD_numbers;

			public ParkAIDebugInfo(int dummy)
			{
				peeps_visible         = 0;
				peeps_animating       = 0;
				peeps_ground_snapping = 0;
				shadows_visible       = 0;
				emotes_showing        = 0;

				LOD_numbers = new Dictionary<PeepLOD, int>();
				foreach (PeepLOD lod in (PeepLOD[])Enum.GetValues(typeof(PeepLOD)))
				{
					LOD_numbers[lod] = 0;
				}
			}

			public void Reset()
			{
				peeps_visible         = 0;
				peeps_animating       = 0;
				peeps_ground_snapping = 0;
				shadows_visible       = 0;
				emotes_showing        = 0;

				foreach (PeepLOD lod in LOD_numbers.Keys.ToList())
				{
					LOD_numbers[lod] = 0;
				}
			}
		}

		public void PerformTotalPeepRedistribution()
		{
			if (_loadedGraph == null || State != SystemState.Running) return;

			for (int i = 0; i < Live.AllPeeps.Count; i++) {
				var agent = AllPeeps[i].Agent;
				agent.TeleportToRandomValidNode(_rand);
			}

		}

		public void OnNodeDisable(RuntimeParkAIGraph graph, Node node)
		{
			DebugLogger.Log($"OnNodeDisable, Graph: {graph}, Node: {node}", LogContext.Pathfinding, LogPriority.Low);

			// Teleport agents out of the node to adjacent nodes
			// Care must be taken to insure the agent does not go to a node that they can't travel out of (if possible)
			// Also must handle agents in the process of traveling to the node.
			{
				for (int i = 0; i < AllPeeps.Count; i++) {
					var agent = AllPeeps[i].Agent;

					if (agent.Location.node == node) {
						agent.TeleportToRandomValidNode(_rand);
					}
				}
			}

		}

		public void OnNodeEnable(RuntimeParkAIGraph graph, Node node)
		{
			DebugLogger.Log($"OnNodeEnable, Graph: {graph}, Node: {node}", LogContext.Pathfinding, LogPriority.Low);

			PerformTotalPeepRedistribution();
		}

		[LuaGlobalFunc]
		public static void parkai_total_redistribution()
		{
			if(Live) Live.PerformTotalPeepRedistribution();
		}

		[LuaGlobalFunc("parkai_node_set_enabled")]
		public static void EnableNode(string nameOrID, bool enabled)
		{
			if (!Live || Live._loadedGraph == null) return;
			if(enabled)
				Live._loadedGraph.EnableNode(nameOrID);
			else
				Live._loadedGraph.DisableNode(nameOrID);
		}

		[LuaGlobalFunc("parkai_node_enable")]
		public static void EnableNode(string nameOrID)
		{
			if (!Live || Live._loadedGraph == null) return;
			Live._loadedGraph.EnableNode(nameOrID);
		}

		[LuaGlobalFunc("parkai_node_disable")]
		public static void DisableNode(string nameOrID)
		{
			if (!Live || Live._loadedGraph == null) return;
			Live._loadedGraph.DisableNode(nameOrID);
		}
	}
}