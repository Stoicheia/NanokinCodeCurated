using System;
using System.Collections.Generic;
using Anjin.UI;
using Anjin.Util;
using UnityEngine;
using Util.Odin.Attributes;

namespace Overworld.UI {

	public class WorldMarkerTester : MonoBehaviour {


		public WorldMarker Template;

		public Transform TestTransform;

		[NonSerialized, ShowInPlay]
		public List<WorldMarker> ActiveMarkers;

		public void Awake()
		{
			ActiveMarkers = new List<WorldMarker>();
		}

		private void Start()
		{
			WorldMarker marker = Template.InstantiateNew();
			marker.transform.SetParent(GameHUD.Live.ElementsRect);
			marker.SetTarget(TestTransform);
			ActiveMarkers.Add(marker);
		}

		private void OnDestroy()
		{
			foreach (WorldMarker marker in ActiveMarkers) {
				marker.gameObject.Destroy();
			}
		}

		/*public struct Marker {
			public HUDElement element;
		}*/

	}
}