using Anjin.Nanokin.Park;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Combat.Startup {
	public class BattleTransitionConfig : SerializedScriptableObject {

		[Title("Defaults")]
		public AudioDef		IntroSound;

		public AnimationCurve EntranceSlowdown;
		public AnimationCurve ExitEnemySlowdown;
		public float          ExitImmunity;

		[Title("Animations", HorizontalLine = false)]
		public CombatIntroAnimationSettings DefaultAnimation;

	}
}