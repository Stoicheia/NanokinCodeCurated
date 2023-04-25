using Anjin.Utils;
using DG.Tweening;

namespace Combat.Data.VFXs
{
	public static class VFXExtensions
	{
		public static TTween SetVFX<TTween>(this TTween tween, VFX vfx)
			where TTween : Tween
		{
			vfx.tweens.Add(tween);

			if (vfx.gameObject != null && vfx.gameObject.TryGetComponent(out TimeScalable timescale))
				tween.timeScale = vfx.gameObject.GetComponent<TimeScalable>().current;

			return tween;
		}
	}
}