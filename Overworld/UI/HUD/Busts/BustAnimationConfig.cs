using Sirenix.OdinInspector;
using UnityEngine;
using Util;

namespace Anjin.UI {
	public class BustAnimationConfig : SerializedScriptableObject {

		public AnimationCurve BlinkRate;
		public FloatRange     BlinkDuration;

		public AnimationCurve BrowWiggleRate;
		public FloatRange     BrowWiggleDuration;

	}
}