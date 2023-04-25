using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util;

namespace Anjin.UI {

	/*public enum LipFlapType {
		Single,   // Open  -> Closed
		Incoming, // Half Open -> Open -> Closed
		Outgoing, // Open -> Half Open -> Closed
		Full      // Half Open -> Open -> Half Open -> Closed
	}*/

	public class LipFlapConfig : SerializedScriptableObject {

		public static AnimationCurve DefaultCurve = new AnimationCurve(
			new Keyframe(0, 0),
			new Keyframe(0.5f, 2.5f),
			new Keyframe(1, 0)
		);

		[FormerlySerializedAs("LipFlapRate")]
		public AnimationCurve LipFlapInbetweenTime;

		[InfoBox("Curves chosen randomly that dictate the duration of the lip flaps and whether to use the half open image or not.\n" +
				 "0 <-> 1: closed\n" +
				 "1 <-> 2: half open\n" +
				 "2 <-> 3: open\n")]
		public List<AnimationCurve> LipFlaps = new List<AnimationCurve>(new []{DefaultCurve});

		/*[InfoBox("Types of lip flaps:" +
				 "Single: Closed -> Open -> Closed" +
				 "Incoming: " +
				 "")]
		public AnimationCurve IncomingLipFlapCurve;
		public AnimationCurve OutgoingLipFlapCurve;
		public AnimationCurve FullCurve;*/
	}
}