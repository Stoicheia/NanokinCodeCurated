using System;
using Anjin.Actors;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.Minigames
{
	public class CoasterObstacle : MonoBehaviour, IHitHandler<SwordHit>
	{
		public enum Sides { Both, Left, Right }

		public Sides Side = Sides.Both;

		[NonSerialized, ShowInPlay]
		public bool Active;

		private void Awake()
		{
			Reset();
		}

		public void Reset()
		{
			Active = true;
		}

		public void OnHit(SwordHit hit)
		{
			Active = false;
		}

		public bool IsHittable(SwordHit hit) => true;
	}
}