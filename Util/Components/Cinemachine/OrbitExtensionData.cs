using System;
using UnityEngine.Playables;

namespace Util.Components.Cinemachine
{
	[Serializable]
	public class OrbitExtensionData : PlayableBehaviour
	{
		public float azimuth;
		public float elevation;
		public float distance;
	}
}