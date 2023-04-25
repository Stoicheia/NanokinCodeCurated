/*using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Nanokin;
using Anjin.Regions;
using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.Cameras
{
	public class CameraControllerOLD : StaticBoyOdin<CameraControllerOLD>
	{
		public GameCameraZoneMetadata PrevCamZoneInside;

		[BoxGroupExt("Main", 0.1f, 0.8f, 1.0f)]
							  public CinemachineBrain       Brain;
		[BoxGroupExt("Main")] public Camera                 UnityCam;
		[BoxGroupExt("Main")] public VCamTarget     GlobalTarget;
		[BoxGroupExt("Main")] public CinemachineTargetGroup GlobalGroupTarget;
		//[BoxGroupExt("Main")] 	public GlobalCameraTarget     CutsceneTarget;

		//public CamRef LastCamRef;

		public CamConfig DefaultPlayerConfig;

		//The thing currently setting the camera configuration
		public ICamController CurrentController 	= null;
		public ICamController OverrideController { get; set; } = null;
		public ICamController PrevController 		= null;

		public VCamProxy CurrentCam = null;
		public VCamProxy PrevCam 	= null;
		public VCamProxy SwitchCam;

		public bool camSwitch;
		public int  camSwitchPriority;

		public Transform TargetsRoot;

		public List<RuntimeCamZone> CamZones;
		public Dictionary<GameCameraZoneMetadata, RuntimeCamZone> ZonesToZones;

		public CinemachineBlenderSettings DefaultBlendSettings;
		public CinemachineBlendDefinition DefaultBlend;


		//	SPAWNED CAMS:
		// 	Cameras that are spawned during runtime. Use a CamRef with an ID to reference these.
		//	Spawned cams could be spawned from a loaded region graph, or an actor who needs
		//	to spawn one.
		//-----------------------------------------------------------------------------------------
		public CamRef 			LastSpawnedCam;
		public List<VCamProxy> 	SpawnedCams;
		public Dictionary<int, VCamProxy> CamIDs;

		public Transform 		SpawnedCamsRoot;


		//-----------------------------------------------------------------------------------------
		//	ACTOR CAMS:
		//	Referenced by ActorRefs. These are cams that we place in the scene manually.
		//-----------------------------------------------------------------------------------------


		//	CALLBACKS:
		//-----------------------------------------------------------------------------------------
		public GameEvent OnCamSpawnPhase;
		public bool InputAffectsCamera => GameController.Live.CanControlPlayer();



		private void Start()
		{
			LastSpawnedCam 	= CamRef.NullRef;
			SpawnedCams 	= new List<VCamProxy>();
			CamIDs 			= new Dictionary<int, VCamProxy>();
			CamZones 		= new List<RuntimeCamZone>();
			ZonesToZones 	= new Dictionary<GameCameraZoneMetadata, RuntimeCamZone>();

			PrevController 	= null;

			GameController.Live.OnFinishLoadLevel_2 += OnFinishLoadLevel_2;
			GameController.Live.OnBeforeLeaveLevel 	+= OnBeforeLeaveLevel;
			GameController.Live.OnEnterGameplay 	+= OnEnterGameplay;

			DefaultBlendSettings = Brain.m_CustomBlends;
			DefaultBlend 		 = Brain.m_DefaultBlend;

			CinemachineVirtualCamera cam;

		}

		void OnEnterGameplay()
		{
			UpdateCurrentController();
		}

		public void OnFinishLoadLevel_2(Level level)
		{
			DoSpawnPhase();
		}


		void OnBeforeLeaveLevel(Level level)
		{
			DoDespawnPhase();
		}

		public void DoSpawnPhase()
		{
			OnCamSpawnPhase?.Invoke();

			//Loop through all loaded graphs and create runtime cam zones for our metadata
			var assets = RegionController.Live.LoadedAssets;
			RegionGraph graph;

			for (int i = 0; i < assets.Count; i++)
			{
				graph = assets[i].Graph;
				for (int j = 0; j < graph.GraphObjects.Count; j++)
				{
					if (!( graph.GraphObjects[j] is RegionObjectSpatial obj )) continue;
					if (obj.Metadata == null) continue;
					for (int k = 0; k < obj.Metadata.Count; k++)
					{
						if (obj.Metadata[k] is GameCameraZoneMetadata cam)
						{
							var runtime = new RuntimeCamZone(obj, cam);
							CamZones.Add(runtime);
							ZonesToZones[cam] = runtime;
						}
					}
				}

				for (int j = 0; j < graph.GlobalMetadata.Count; j++)
				{
					if (graph.GlobalMetadata[j] is GameCameraZoneMetadata cam)
					{
						var runtime = new RuntimeCamZone(cam);
						CamZones.Add(runtime);
						ZonesToZones[cam] = runtime;
					}
				}
			}

			for (int i = 0; i < CamZones.Count; i++)
			{
				SpawnCamsForZone(CamZones[i]);
			}
		}

		public void DoDespawnPhase()
		{
			CamZones.Clear();
		}

		public void SpawnCamsForZone(RuntimeCamZone zone)
		{
			if (zone == null || zone.data == null) return;
			var (reference, cam) =  SpawnCamFromConfig(zone.data.Config, zone);

			zone.Cams.Add(reference);

		}

		public (CamRef, VCamProxy) SpawnCamFromConfig(CamConfig config, RuntimeCamZone zone)
		{
			if (config.CamType != CamConfig.Type.Spawned) return (CamRef.NullRef, null);

			VCamProxy proxy = null;

			switch (config.CamVariation)
			{
				case CameraVariation.HOrbit: 		proxy = NewHOrbitCam(SpawnedCamsRoot); 		break;
				case CameraVariation.Static: 		proxy = NewStaticCam(SpawnedCamsRoot); 		break;
				case CameraVariation.FixedOffset: 	proxy = NewFixedOffsetcam(SpawnedCamsRoot); break;
			}

			if (proxy)
			{
				proxy.Follow = GlobalTarget.transform;
				proxy.LookAt = GlobalTarget.transform;
			}

			//TODO: Do all the relative stuff!!!
			if (config.SpawnParams != null) {
				if (zone.obj is RegionObjectSpatial spatial)
					proxy.transform.position = spatial.Transform.Position + config.SpawnParams.Position;
			}

			var reference = RegisterSpawnedCam(proxy);

			return (reference, proxy);
		}

		public CamRef RegisterSpawnedCam(VCamProxy cam)
		{
			var ID = LastSpawnedCam.ID + 1;
			SpawnedCams.Add(cam);
			CamIDs[ID] = cam;
			cam.reference = new CamRef(ID);

			LastSpawnedCam = cam.reference;
			return cam.reference;
		}

		public CinemachineVirtualCamera NewVCam(Transform root)	=> Instantiate(GameAssets.Live.CamPrefab_Base,	  root);
		public VCamProxy NewHOrbitCam(Transform root)		=> Instantiate(GameAssets.Live.CamPrefab_HOrbit,	  root);
		public VCamProxy NewStaticCam(Transform root)		=> Instantiate(GameAssets.Live.CamPrefab_Static,	  root);
		public VCamProxy NewFixedOffsetcam(Transform root) 	=> Instantiate(GameAssets.Live.CamPrefab_FixedOffset, root);

		public VCamProxy GetCam(CamRef reference)
		{
			return CamIDs[reference.ID];
		}

		void Update()
		{
			//Decide who's controlling
			/*var zone = RegionController.Live.CamZoneInside;
			if (PrevCamZoneInside != zone || PrevController != CurrentController)
			{
				UpdateCurrentController(zone);
				PrevController = CurrentController;
			}#1#

			// Don't change the camera when we're wraping out.

			UpdateCurrentController();

			if (camSwitch) 	UpdateCamSwitch();
			else 			UpdateCamPriorities();
		}

		void UpdateCamSwitch()
		{
			if (CurrentCam == null) {
				camSwitchPriority = 0;
				camSwitch         = false;
				return;
			}

			CurrentCam.Priority = ( CurrentCam.Priority == camSwitchPriority )
				? CurrentCam.Priority = camSwitchPriority + 1
				: CurrentCam.Priority = camSwitchPriority;

			//We need to check here, because Brain.ActiveVirtualCamera will be null on the first frame
			if (Brain.ActiveVirtualCamera == null) return;
			if (CurrentCam.Getcam() != Brain.ActiveVirtualCamera) return;

			CurrentCam.Priority = camSwitchPriority;
			camSwitchPriority   = 0;
			camSwitch           = false;
		}

		public void UpdateCurrentController()
		{
			if (GameController.Live.StateGame == GameController.GameState.WarpOut && !( OverrideController is TransitionBrain )) return;

			if (OverrideController != null)
				CurrentController = OverrideController;
			else if (RegionController.Live.CamZoneInside == null)
				CurrentController = null;
			else
				CurrentController = ZonesToZones[RegionController.Live.CamZoneInside];

			if (CurrentController != PrevController)
			{
				if(PrevController != null && PrevController is ICamControllerOwner owner)
					owner.DeactivateCams(p_InbuiltDormant);

				PrevController = CurrentController;
			}
		}

		public void UpdateCamPriorities()
		{
			//Deactivate all known cameras, then activate the one we want.
			DeactivateSpawnedCameras();

			CamConfig config = null;

			Brain.m_CustomBlends = DefaultBlendSettings;
			Brain.m_DefaultBlend = DefaultBlend;

			switch (CurrentController)
			{
				case ICamControllerOwner owner:
					//TODO: Add a "controller changed" system so we don't do this all the time.
					if (owner.OverridesBlends) {
						Brain.m_CustomBlends = owner.Blends;
						Brain.m_DefaultBlend = owner.DefaultBlend;
					}

					owner.DeactivateCams(p_InbuiltDormant);
					owner.UpdateCams(p_InbuiltActive, p_InbuiltDormant);
				break;

				// case PlayerControlBrain plr:
				// 	config = DefaultPlayerConfig;
				//
				// 	switch (config.CamVariation)
				// 	{
				// 		case CameraVariation.HOrbit:
				// 			CurrentCam = GlobCam_HOrbit;
				// 			GlobCam_HOrbit.SetFromConfig(config);
				// 		break;
				//
				// 		case CameraVariation.FixedOffset:
				// 			//GlobCam_FixedOffset.Priority = p_InbuiltActive;
				// 			CurrentCam = GlobCam_FixedOffset;
				// 			GlobCam_FixedOffset.SetFromConfig(config);
				// 		break;
				// 	}
				//
				// 	CurrentCam.IgnoreFollow = GameController.Live.IsWarping;
				// break;

				case RuntimeCamZone camzone:
					config = camzone.data.Config;

					for (int i = 0; i < camzone.Cams.Count; i++)
					{
						var cam = GetCam(camzone.Cams[i]);
						CurrentCam = cam;
						cam.Priority = p_InbuiltActive;
						cam.SetFromConfig(config);
					}
				break;

				default:
					CurrentCam = null;
					break;

				/*case Cutscene cutscene:
					config = cutscene.CameraConfig;
				break;#1#
			}

			if (PrevCam == CurrentCam)
			{
				if (CurrentCam != null)
				{
					CurrentCam.Priority = p_InbuiltActive;
				}
			}
			else if (!camSwitch)
			{
				camSwitch = true;
				camSwitchPriority = p_InbuiltActive;
				PrevCam = CurrentCam;
			}

			if (config != null)
			{
				if (config.Targeting == CamConfig.TargetType.Player) {
					GlobalTarget.point = DefaultPlayerConfig.TargetWorldPoint;
				}
				else if(config.Targeting == CamConfig.TargetType.WorldPoint)
					GlobalTarget.point = config.TargetWorldPoint;
			}
		}

		[FoldoutGroup("Locks")]           public bool                               LockToInbuilt;
		[FoldoutGroup("Locks")]           public CinemachineVirtualCameraBase       LockToCamera;
		[FoldoutGroup("Locks")]           public int                                PrevLockToCameraPriority;

		[FoldoutGroup("Priority Config")] public int                                p_InbuiltDormant;
		[FoldoutGroup("Priority Config")] public int                                p_InbuiltActive;
		[FoldoutGroup("Priority Config")] public int                                p_LevelDormant;
		[FoldoutGroup("Priority Config")] public int                                p_LevelActive;
		[FoldoutGroup("Priority Config")] public int                                p_LockToCam;

		[FoldoutGroup("Rendering")] public CameraRendertextureInput BrainRTOutput_Main;
		[FoldoutGroup("Rendering")] public CameraRendertextureInput BrainRTOutput_Transparent;

		public void DeactivateSpawnedCameras()
		{
			for (int i = 0; i < SpawnedCams.Count; i++)
			{
				SpawnedCams[i].Priority = p_InbuiltDormant;
			}
		}

		public static (RaycastHit, bool) RaycastFromCamera(Vector2 screenPoint, int layerMask, float maxDistance = 10000)
		{
			if(!Live.UnityCam) return ( new RaycastHit(), false );

			RaycastHit hit;
			var ray = Live.UnityCam.ScreenPointToRay(screenPoint);

			if (Physics.Raycast(ray, out hit,maxDistance,layerMask))
			{
				return ( hit, true );
			}

			return ( new RaycastHit(), false );
		}
	}

}*/