using System;
using Sirenix.OdinInspector;

namespace Anjin.Cameras
{
	public class CamSpawner : SerializedMonoBehaviour
	{
		public CamConfig config;

		[NonSerialized, HideInEditorMode]
		public CamRef SpawnedCam;
		//public List<CamRef> SpawnedCams;

		void Start()
		{
			//SpawnedCams = new List<CamRef>();
			/*var (reference, cam) = GameCams.Live.SpawnCamFromConfig(config, null);
			SpawnedCam = reference;*/

		}

		public bool ActiveOverride => true;
	}
}