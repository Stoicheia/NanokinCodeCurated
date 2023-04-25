using DG.Tweening;
using JetBrains.Annotations;
using Overworld.Cutscenes;
using UnityEngine;
using Util.UniTween.Value;

namespace Combat.Toolkit.Camera
{
	public abstract class CoroutineCamAnim<T> : CoroutineManaged
	{
		public    T                targetValue;
		public    float            duration;
		public    Ease             ease;
		protected ArenaCamera      cam;
		public    TweenableValue<T> remainingChange;
		protected   T                _last;

		public override bool Active => remainingChange != null && remainingChange.IsTweenActive;

		public override void OnStart()
		{
			cam = costate.battle.arena.Camera;

			remainingChange = GetTweener();
			remainingChange.To(default, new EaserTo(duration, ease));
			_last = remainingChange.value;
		}

		public override void OnCoplayerUpdate(float dt)
		{
			ApplyChange(GetChange());
			_last = remainingChange.value;
		}

		protected abstract TweenableValue<T> GetTweener();
		protected abstract T                 GetChange();


		protected abstract T             GetCurrentValue();

		protected abstract void ApplyChange(T chg);
	}

	public abstract class CoroutineCamFloatAnim : CoroutineCamAnim<float> {
		[NotNull] protected override TweenableValue<float> GetTweener() => new TweenableFloat(targetValue - GetCurrentValue());
		protected override  float                 GetChange()  => _last - remainingChange;
	}

	public abstract class CoroutineCamVector3Anim : CoroutineCamAnim<Vector3> {
		[NotNull] protected override TweenableValue<Vector3> GetTweener() => new TweenableVector3(targetValue - GetCurrentValue());
		protected override  Vector3                 GetChange()  => _last - remainingChange;
	}
}