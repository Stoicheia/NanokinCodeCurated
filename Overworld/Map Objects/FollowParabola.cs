using System;
using UnityEngine;
using Util;
using Util.Odin.Attributes;

namespace Anjin.Nanokin.Map {
	public class FollowParabola : MonoBehaviour {

		public Vector3 Start;
		public Vector3 End;
		public float   Height;

		public float Speed;

		[NonSerialized, ShowInPlay]
		public float Time = 0;

		[NonSerialized, ShowInPlay]
		public bool Following = true;

		public void Update()
		{
			if (GameController.IsWorldPaused) return;

			if(Following) {
				float length = MathUtil.ParabolaLength(Height, Vector3.Distance(Start, End));
				Time += (Speed * UnityEngine.Time.deltaTime) / length;

				if (Time >= 1) {
					Following          = false;
					transform.position = MathUtil.EvaluateParabola(Start, End, Height, 1);
					return;
				}

				transform.position = MathUtil.EvaluateParabola(Start, End, Height, Time);
			}
		}

	}
}