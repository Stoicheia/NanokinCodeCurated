using DG.Tweening;

namespace Util.Components.Cinemachine
{
	public class AdditiveTweenOrbit : AdditiveOrbit
	{
		private Tween _tween;

		public override bool Expired => _tween == null || !_tween.active;

		public void RemoveOnComplete(Tween tween)
		{
			_tween = tween;
		}
	}
}