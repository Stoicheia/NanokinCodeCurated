using System;
using Anjin.Util;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Util.UniTween.Value;

namespace Combat.UI
{
	public class ComboLabel : SerializedMonoBehaviour
	{
		[SerializeField] public TextMeshProUGUI Label;

		[NonSerialized] public TweenableColor   color;
		[NonSerialized] public TweenableVector3 scale;
		[NonSerialized] public RectTransform    rectTransform;

		private void Awake()
		{
			rectTransform = GetComponent<RectTransform>();

			color = new TweenableColor(Color.white, AlreadyTweeningBehaviors.Complete);
			scale = new TweenableVector3(Vector3.one, AlreadyTweeningBehaviors.Complete);
		}

		public Sequence Play(Animation anim)
		{
			Sequence sequence = DOTween.Sequence();

			color?.To(anim.ColorTween).JoinTo(sequence);
			scale?.To(anim.ScaleTween).JoinTo(sequence);

			return sequence;
		}

		public void Reset()
		{
			Label.rectTransform.localScale = Vector3.one;
			Label.color                    = Color.white.To32();
		}

		private void Update()
		{
			Label.rectTransform.localScale = scale.value;
			Label.color                    = color.value;
		}

		[Serializable]
		public class Animation
		{
			[FormerlySerializedAs("_scaleTween")]
			public Vector3Tween ScaleTween;

			[FormerlySerializedAs("_colorTween")]
			public ColorTween ColorTween;
		}
	}
}