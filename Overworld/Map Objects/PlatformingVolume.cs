using UnityEngine;
using Util;

namespace Anjin.Nanokin.Map {

	// Anything that effects how the player moves without putting them in a specific state
	public class PlatformingVolume : MonoBehaviour {

		public bool RequiresGrounding = false;

		public bool          SpeedBoost;
		public Option<float> SpeedBoostMultiplier = 1;
		public Option<float> SpeedBoostDuration = 1;




	}
}