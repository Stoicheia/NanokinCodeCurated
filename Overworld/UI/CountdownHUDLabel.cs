using System;
using System.Collections.Generic;
using Anjin.UI;
using DG.Tweening;
using DG.Tweening.Core;
using Sirenix.OdinInspector;
using TMPro;
using UniTween.Core;
using UnityEngine;
using Util;
using Util.Extensions;
using Util.Odin.Attributes;
using Util.UniTween.Value;
using Random = UnityEngine.Random;

namespace Overworld.UI {
	public class CountdownHUDLabel : HUDElement, IRecyclable {

		public enum Animations {
			None,
			FlyUp
		}

		[ShowInPlay]
		public TMP_Text  Text;

		[ShowInPlay]
		private Animation _anim;

		[ShowInPlay]
		private List<AnimationState> _states;

		[ShowInPlay]
		public bool IsPlaying;

		protected override void Awake()
		{
			base.Awake();
			Text = GetComponentInChildren<TMP_Text>();
			_anim = GetComponent<Animation>();

			_states = new List<AnimationState>();

			foreach (AnimationState state in _anim) {
				//print(name + ": " +state.clip);
				_states.Add(state);
			}

			IsPlaying = false;
		}

		private void Update()
		{
			if (!_anim.isPlaying) {
				IsPlaying = false;
			}
		}

		[Button]
		public void DoAnimation(Animations anim = Animations.None, float? duration = null)
		{
			if (_states.Count <= 0) return;

			if(duration.HasValue)
				_states[0].normalizedSpeed = 1 / duration.Value;

			if (_anim.isPlaying) {
				_anim.Stop();
				_anim.Rewind();
			}

			switch (anim) {
				case Animations.FlyUp:
					DefaultFlyUp(duration.GetValueOrDefault(1), Ease.InOutQuad);
					break;
			}

			_anim.Play();

			IsPlaying = true;
		}

		[Button]
		public void DefaultFlyUp(float duration = 1, Ease ease = Ease.Linear) => FlyUp(new FloatRange(-175, 175), new FloatRange(50, 150), duration, ease);

		[Button]
		public void FlyUp(FloatRange hRange, FloatRange vRange, float duration = 1, Ease ease = Ease.Linear)
		{

			Sequence seq = DOTween.Sequence();

			var h = hRange.RandomInclusive;

			Vector3 xTarget = new Vector3(Mathf.Sign(h) * 100 + h, 0);
			Vector3 yTarget = new Vector3(0, vRange.RandomInclusive);

			{
				Vector3 initial = Random.insideUnitCircle * 25;
				SequenceOffset = initial;

				SequenceOffset.SetupForTweeningIfNecessary();
				DOGetter<Vector3> g = SequenceOffset.getter;
				DOSetter<Vector3> s = SequenceOffset.setter;

				seq.Append(DOTween.To(g, s, initial + xTarget, duration)
								  .SetEase(Ease.OutQuad)
								  .SetOptions(AxisConstraint.X));

				seq.Join(DOTween.To(g, s, initial + yTarget, duration)
									.SetEase(Ease.InQuad)
									.SetOptions(AxisConstraint.Y));
			}

			{
				float z = Mathf.Sign(h) * -(15 + Random.value * 25);

				Vector3 initial = new Vector3(0, 0, z);
				Rotation = initial;

				Rotation.SetupForTweeningIfNecessary();
				DOGetter<Vector3> g = Rotation.getter;
				DOSetter<Vector3> s = Rotation.setter;

				seq.Join(DOTween.To(g, s, new Vector3(0, 0, 0), duration))
								.SetEase(Ease.OutQuad);
			}

			seq.OnComplete(Recycle).SetEase(ease);
		}

		public void Recycle()
		{
			SequenceOffset.EnsureComplete();
			SequenceOffset = Vector3.zero;
			IsPlaying      = false;
		}
	}
}