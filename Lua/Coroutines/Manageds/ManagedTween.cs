using DG.Tweening;
using Overworld.Cutscenes;

namespace Anjin.Scripting.Waitables
{
	[LuaUserdata]
	public class ManagedTween : CoroutineManaged
	{
		public readonly Tween Tween;

		public ManagedTween(Tween tween)
		{
			Tween = tween;
		}

		public override bool Active => Tween != null && Tween.active;

		public override void OnEnd(bool forceStopped , bool skipped = false)
		{
			Tween.Complete();
		}
	}
}