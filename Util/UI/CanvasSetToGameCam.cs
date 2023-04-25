using Anjin.Cameras;
using UnityEngine;

namespace Anjin.EditorUtility {
	public class CanvasSetToGameCam : MonoBehaviour {

		private void Start()
		{
			if (GameCams.Exists && transform.TryGetComponent(out Canvas canvas)) {
				canvas.worldCamera = GameCams.Live.UnityCam;
			}
		}

	}
}