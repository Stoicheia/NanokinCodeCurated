using Cysharp.Threading.Tasks;
using DG.Tweening;
using JetBrains.Annotations;
using UnityEngine;

namespace Combat.Toolkit
{
	public class DoTweenAnim : BattleAnim
	{
		protected Tween tween;

		protected DoTweenAnim([CanBeNull] Tween tween = null)
		{
			this.tween = tween;
			tween?.Pause();
		}

		public override void RunInstant()
		{
			Debug.LogWarning("Skipping a DOTween animation in ExecuteInstant...");
		}

		public override async UniTask RunAnimated()
		{
			await tween.WithCancellation(cts.Token).SuppressCancellationThrow();
		}
	}
}