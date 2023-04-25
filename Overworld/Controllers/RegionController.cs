using System;
using System.Collections.Generic;
using Anjin.Actors;
using Anjin.Scripting;
using Anjin.Scripting.Waitables;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Util.Odin.Attributes;
using Vexe.Runtime.Extensions;

namespace Anjin.Regions
{
	public class RegionController : StaticBoy<RegionController>
	{
		public const int MAX_METADATA      = 128;
		public const int MAX_PLAYER_INSIDE = 32;
		public const int MAX_AUDIO_ZONES   = 64;
		public const float REGION_COL_UPDATE_COOLDOWN = 0.1f;

		private float _timeOfNextUpdate;

		// Database stuff
		[NonSerialized, ShowInPlay] public static List<RegionGraphAsset>      loadedAssets;
		[NonSerialized, ShowInPlay] public static List<SceneRegionObjectBase> trackedSceneObjects;

		[NonSerialized, ShowInPlay] public RegionMetadata[] metadatas;
		[NonSerialized, ShowInPlay] public int              metadatasNum;

		[NonSerialized, ShowInPlay] public RegionObjectSpatial    camShapeInside;
		[NonSerialized, ShowInPlay] public GameCameraZoneMetadata camZoneInside;
		[NonSerialized, ShowInPlay] public int                    numAudioZones;
		[NonSerialized, ShowInPlay] public RegionObjectSpatial[]  audioShapesInside;
		[NonSerialized, ShowInPlay] public AudioZoneMetadata[]    audioZonesInside;

		[ShowInPlay] private Dictionary<string, RegionObject> _loadedObjects;
		[ShowInPlay] private Dictionary<string, RegionObject> _loadedObjectsNames;
		[ShowInPlay] private RegionObjectSpatial[]            _playerInside;
		[ShowInPlay] private int                              _playerInsideNum;

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			loadedAssets        = null;
			trackedSceneObjects = new List<SceneRegionObjectBase>();
		}

		protected override void OnAwake()
		{
			base.OnAwake();

			loadedAssets        = new List<RegionGraphAsset>();
			//trackedSceneObjects = new List<SceneRegionObjectBase>();

			_loadedObjects      = new Dictionary<string, RegionObject>();
			_loadedObjectsNames = new Dictionary<string, RegionObject>();
			_playerInside       = new RegionObjectSpatial[MAX_PLAYER_INSIDE];
			_playerInsideNum    = 0;

			metadatas    = new RegionMetadata[MAX_METADATA];
			metadatasNum = 0;

			audioShapesInside = new RegionObjectSpatial[MAX_AUDIO_ZONES];
			audioZonesInside  = new AudioZoneMetadata[MAX_AUDIO_ZONES];

			_timeOfNextUpdate = 0;
		}

		public static void OnLevelLoad(LevelManifest manifest)
		{
			loadedAssets.AddRange(manifest.RegionGraphs);
			Live.UpdateDB();
		}

		public static void OnLevelExit()
		{
			loadedAssets.Clear();
			trackedSceneObjects.Clear();
			Live.UpdateDB();
		}

		private void UpdateDB()
		{
			_loadedObjects.Clear();

			foreach (RegionGraphAsset asset in loadedAssets)
				AddGraphObjs(asset);
		}

		public static void AddGraph(RegionGraphAsset asset)
		{
			if (loadedAssets.Contains(asset)) return;

			loadedAssets.Add(asset);
			AddGraphObjs(asset);
		}

		static void AddGraphObjs(RegionGraphAsset asset)
		{
			RegionGraph graph = asset.Graph;

			void regObj(RegionObject robj)
			{
				Live._loadedObjects[robj.ID] = robj;

				if(!robj.Name.IsNullOrWhitespace())
					Live._loadedObjectsNames[robj.Name] = robj;
			}

			foreach (RegionObject obj 			in graph.GraphObjects) 	regObj(obj);
			foreach (RegionLink obj 			in graph.Links) 		regObj(obj);
			foreach (RegionSpatialLinkBase obj 	in graph.SpatialLinks) 	regObj(obj);
		}

		private void Update()
		{
			if (_timeOfNextUpdate <= Time.time)
			{
				UpdatePlayerInside();
				_timeOfNextUpdate = Time.time + REGION_COL_UPDATE_COOLDOWN;
			}

			camZoneInside = null;
			numAudioZones = 0;

			for (int i = 0; i < MAX_AUDIO_ZONES; i++)
			{
				audioShapesInside[i] = null;
				audioZonesInside[i]  = null;
			}

			// Filter out key metadata
			for (int i = 0; i < _playerInsideNum; i++)
			{
				RegionObjectSpatial obj = _playerInside[i];
				if (obj.Metadata == null || obj.Metadata.IsEmpty())
					continue;

				for (int j = 0; j < obj.Metadata.Count; j++)
				{
					RegionMetadata meta = obj.Metadata[j];

					if (meta is GameCameraZoneMetadata cam)
					{
						if (camZoneInside == null ||
							cam.Priority > camZoneInside.Priority)
						{
							camZoneInside  = cam;
							camShapeInside = obj;
						}
					}

					if (meta is AudioZoneMetadata audio)
					{
						if (numAudioZones < MAX_AUDIO_ZONES)
						{
							audioShapesInside[numAudioZones] = obj;
							audioZonesInside[numAudioZones]  = audio;
							numAudioZones++;
						}
					}
				}
			}

			for (int i = 0; i < metadatasNum; i++)
			{
				RegionMetadata meta = metadatas[i];

				if (meta is GameCameraZoneMetadata cam)
				{
					if (camZoneInside == null ||
						cam.Priority > camZoneInside.Priority)
					{
						camShapeInside = null;
						camZoneInside  = cam;
					}
				}

				if (meta is AudioZoneMetadata audio)
				{
					if (numAudioZones < MAX_AUDIO_ZONES)
					{
						audioShapesInside[numAudioZones] = null;
						audioZonesInside[numAudioZones]  = audio;
						numAudioZones++;
					}
				}
			}
		}

		public void UpdatePlayerInside()
		{
			//Clearing for legibility in the inspector. Maybe remove for performance later.
			for (int i = 0; i < _playerInside.Length; i++)
				_playerInside[i] = null;

			_playerInsideNum = 0;
			metadatasNum     = 0;

			//Find all graph objects that the player is inside.
			Actor plr = ActorController.playerActor;
			if (plr == null) return;

			//TODO: Don't hardcode this and instead find the center of the actor another way.
			Vector3     pos = plr.transform.position + new Vector3(0, 0.5f, 0);
			RegionGraph graph;

			/*using(Draw.ingame.WithDuration(REGION_COL_UPDATE_COOLDOWN))
				Draw.ingame.WireSphere(pos, 0.2f, Color.red);*/

			//TODO: Find a better way to limit this so we don't loop through literally every graph.
			for (int i = 0; i < loadedAssets.Count; i++)
			{
				if (_playerInsideNum >= MAX_PLAYER_INSIDE - 1) break;
				graph = loadedAssets[i].Graph;
				for (int j = 0; j < graph.GraphObjects.Count; j++)
				{
					//Debug.Log(graph.GraphObjects[j].Name);
					if (graph.GraphObjects[j] is RegionObjectSpatial obj)
					{
						if (obj.IsPointOverlapping(pos))
						{
							_playerInside[_playerInsideNum++] = obj;
						}
					}
				}

				for (int j = 0; j < graph.GlobalMetadata.Count; j++)
				{
					metadatas[metadatasNum++] = graph.GlobalMetadata[j];
				}
			}
		}

		/// <summary>
		/// Find an object by either [object_name_or_id] or <Graph Asset Name>/[object_name_or_id]
		/// </summary>
		/// <param name="full_path"></param>
		/// <returns></returns>
		[Button]
		public static async UniTask<RegionObject> GetRegionObject(string full_path)
		{
			string obj = full_path;
			if (full_path.Contains(":")) {
				int    ind   = full_path.IndexOf(":");
				string graph = "Regions/" + full_path.Substring(0,  ind);
				obj   = full_path.Substring(ind + 1);

				RegionGraphAsset asset = await GameAssets.LoadAsset<RegionGraphAsset>(graph);
				AddGraph(asset);
			}

			RegionObject robj;

			// Find by name
			if (Live._loadedObjectsNames.TryGetValue(obj, out robj))
				return robj;

			// Find by ID
			if (Live._loadedObjects.TryGetValue(obj, out robj))
				return robj;

			return null;
		}

		[LuaGlobalFunc]
		public WaitableUniTask get_region_object(string full_path)
		{
			//NOTE: Not sure how to do this cleanly
			throw new NotImplementedException();
		}

		public void LoadGraph(string asset_address)
		{
			Addressables.LoadAssetAsync<RegionGraphAsset>(asset_address).Completed += handle =>
			{
				if (handle.Result) loadedAssets.Add(handle.Result);
			};
		}

		public RegionObject GetByID(string path)
		{
			return _loadedObjects.ValueOrDefault(path, null);
		}

		//Todo: Make non allocating!
		public List<RegionObjectSpatial> GetGraphObjectsAtPoint(Vector3 point)
		{
			List<RegionObjectSpatial> list = new List<RegionObjectSpatial>();
			RegionGraph               graph;
			for (int i = 0; i < loadedAssets.Count; i++)
			{
				graph = loadedAssets[i].Graph;
				for (int j = 0; j < graph.GraphObjects.Count; j++)
				{
					if (graph.GraphObjects[j] is RegionObjectSpatial obj)
					{
						if (obj.IsPointOverlapping(point))
						{
							list.Add(obj);
						}
					}
				}
			}

			return list;
		}
	}
}