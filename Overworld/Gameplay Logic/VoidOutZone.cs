using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.Nanokin.Park {



	public class VoidOutZone : MonoBehaviour {

		[Inline()]
		public OverworldDeathConfig Config = new OverworldDeathConfig{mode = OverworldDeathConfig.Mode.LastCheckpointOrValidGrounding};

	}
}