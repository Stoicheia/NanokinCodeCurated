using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityUtilities;
using Util;

namespace Overworld.Rendering
{
	public class AfterImageEffect : MonoBehaviour, IRecyclable
	{
		[FormerlySerializedAs("rend")]
		public SpriteRenderer Renderer;

		[FormerlySerializedAs("sprite")]
		public Sprite Sprite;

		public float StartingLife = 1;

		[FormerlySerializedAs("alphaCurve")]
		public AnimationCurve AlphaCurve;

		[NonSerialized]
		public ComponentPool<AfterImageEffect> parentPool;

		[NonSerialized]
		public float life;

		private void Awake()
		{
			life = StartingLife;
		}

		private void Start()
		{
			Renderer.sprite = Sprite;
		}

		public void Recycle()
		{
			life = StartingLife;
		}

		private void Update()
		{
			if (life > 0)
			{
				Renderer.color = Renderer.color.Change(a: AlphaCurve.Evaluate(1 - life / StartingLife));

				life -= Time.deltaTime;
				if (life <= 0)
				{
					life = StartingLife;
					parentPool?.Return(this);
				}
			}
		}
	}
}