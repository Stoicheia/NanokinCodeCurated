using Sirenix.OdinInspector;
using UnityEngine;
using Util;

namespace Anjin.Minigames
{
	public class CoasterConfig : SerializedScriptableObject
	{
		public Overridable<float> StartingSpeed;
		public Overridable<float> MinSpeed;
		public Overridable<float> MaxSpeed;

		public Overridable<float> Friction;
		public Overridable<float> Gravity;

		public Overridable<float> SlopeRange;

		public AnimationCurve     SpeedAccelleration = new AnimationCurve();
		public AnimationCurve     SpeedDecelleration = new AnimationCurve();

		public float	MaxTiltAngle;
		public Vector2	MaxTiltOffset;

		public float          HopDurationPerMeter = 0.15f;
		public AnimationCurve HopDurationOverDistance;
		public AnimationCurve HopLateral;
		public AnimationCurve HopVertical;

		public CoasterCarActor.SwordSwingState.Settings Swing;

	}
}