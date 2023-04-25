using JetBrains.Annotations;
using Sirenix.OdinInspector;

namespace Combat.Entities
{
	public class TweenMovable : SerializedMonoBehaviour
	{
		[InfoBox("Motion types are defined in std-anim.lua")]
		[CanBeNull] public string BaseMove;
	}
}