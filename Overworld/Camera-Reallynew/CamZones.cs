using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Nanokin;
using Anjin.Regions;
using Anjin.Util;
using Cinemachine;
using Pathfinding.Util;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Cameras
{
	public class CamZones : StaticBoy<CamZones>, ICamController
	{
		[NonSerialized, ShowInInspector, HideInEditorMode]
		public List<Zone> Zones;
		[NonSerialized, ShowInInspector, HideInEditorMode]
		public Zone CurrentZone;
		[NonSerialized, ShowInInspector, HideInEditorMode]
		public Dictionary<GameCameraZoneMetadata, int> MetadataToZones;
		[NonSerialized, ShowInInspector, HideInEditorMode]
		public GameEvent OnCamSpawnPhase;

		protected override void OnAwake()
		{
			Zones           = new List<Zone>();
			MetadataToZones = new Dictionary<GameCameraZoneMetadata, int>();

			GameController.Live.OnFinishLoadLevel_2 += CamSpawnPhase;
			GameController.Live.OnBeforeLeaveLevel  += CamDespawnPhase;
		}

		private void CamDespawnPhase(Level level)
		{
			for (int i = 0; i < Zones.Count; i++)
				Zones[i].Cleanup();
			Zones.Clear();
		}

		private void CamSpawnPhase(Level level)
		{
			OnCamSpawnPhase?.Invoke();

			//Loop through all loaded graphs and create runtime cam zones for our metadata
			var         assets = RegionController.loadedAssets;
			RegionGraph graph;

			void addCam(GameCameraZoneMetadata data, RegionObjectSpatial obj)
			{
				var cam = data.Config.SpawnVCam(transform.root, obj);

				var zone = new Zone(data, obj);
				zone.Cameras.Add(cam);
				Zones.Add(zone);

				if (data.Config.ConfineToBox)
				{
					var go = new GameObject("Zone Confiner");
					go.transform.SetParent(transform);

					var mat = data.Config.GetConfinementMatrix(obj);
					go.transform.position = mat.MultiplyPoint3x4(Vector3.zero);
					go.transform.rotation = mat.rotation;

					var collider = go.AddComponent<BoxCollider>();

					collider.center  = data.Config.ConfinementParams.Center;
					collider.size    = data.Config.ConfinementParams.Size;
					zone.Confinement = collider;

					var confiner = cam.AddComponent<CinemachineConfiner>();
					confiner.m_ConfineMode        = CinemachineConfiner.Mode.Confine3D;
					confiner.m_BoundingVolume     = collider;
					confiner.m_ConfineScreenEdges = false;
					cam.AddExtension(confiner);
				}

				MetadataToZones[data] = Zones.Count - 1;
			}

			for (int i = 0; i < assets.Count; i++)
			{
				graph = assets[i].Graph;
				for (int j = 0; j < graph.GraphObjects.Count; j++)
				{
					if (!(graph.GraphObjects[j] is RegionObjectSpatial obj)) continue;
					if (obj.Metadata == null) continue;

					for (int k = 0; k < obj.Metadata.Count; k++)
						if (obj.Metadata[k] is GameCameraZoneMetadata cam)
							addCam(cam, obj);
				}

				for (int j = 0; j < graph.GlobalMetadata.Count; j++)
				{
					if (graph.GlobalMetadata[j] is GameCameraZoneMetadata cam)
						addCam(cam, null);
				}
			}
		}

		private void Update()
		{
			if (GameController.Live.StateGame != GameController.GameState.MidWarp &&
				(GameCams.Live._controller == null || GameCams.Live._controller is PlayerCameraRig))
			{
				if (RegionController.Live.camZoneInside != null)
				{
					GameCams.SetController(this);

					if (MetadataToZones.TryGetValue(RegionController.Live.camZoneInside, out var index))
						CurrentZone = Zones[index];
				}
			}
		}

		public void OnActivate() { }

		public void OnRelease(ref CinemachineBlendDefinition? blend)
		{
			blend = CurrentZone.Data.Config.BlendOutgoing;

			for (int i = 0; i < Zones.Count; i++)
			for (int j = 0; j < Zones[i].Cameras.Count; j++)
				Zones[i].Cameras[j].Priority = GameCams.PRIORITY_INACTIVE;
		}

		public void ActiveUpdate()
		{
			// Deactivate all zones, then activate the one the player is inside
			//---------------------------------------------------------------------------------------
			for (int i = 0; i < Zones.Count; i++)
			for (int j = 0; j < Zones[i].Cameras.Count; j++)
				Zones[i].Cameras[j].Priority = GameCams.PRIORITY_INACTIVE;

			if (RegionController.Live.camZoneInside != null)
			{
				if(CurrentZone.Cameras != null) {
					for (int i = 0; i < CurrentZone.Cameras.Count; i++)
						CurrentZone.Cameras[i].Priority = GameCams.PRIORITY_ACTIVE;
				}
			}
			else
			{
				GameCams.ReleaseController();
			}
		}

		public void GetBlends(ref CinemachineBlendDefinition? blend, ref CinemachineBlenderSettings settings)
		{
			if (RegionController.Live.camZoneInside != null && CurrentZone.Data != null)
			{
				blend = CurrentZone.Data.Config.BlendIncoming;
			}
		}

		public struct Zone
		{
			public List<CinemachineVirtualCamera> Cameras;
			public RegionObject                   RegionObject;
			public GameCameraZoneMetadata         Data;
			public Collider                       Confinement;

			public Zone(GameCameraZoneMetadata data, RegionObject obj) : this()
			{
				RegionObject = obj;
				Data         = data;
				Cameras      = ListPool<CinemachineVirtualCamera>.Claim(10);
			}

			public void Cleanup()
			{
				if (Confinement != null)
					Confinement.gameObject.Destroy();
				ListPool<CinemachineVirtualCamera>.Release(Cameras);
			}
		}
	}
}