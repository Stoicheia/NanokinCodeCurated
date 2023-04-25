using Combat.Data.VFXs;
using UnityEngine;

namespace Combat.Toolkit
{
	public class ShakeVFX : VFX
	{
		public float duration   = float.MaxValue; // Must be manually stopped by default
		public float amplitude  = 0.16f;
		public float speed      = 25;
		public float randomness = 0;

		private float _elapsed;

		public override Vector3 VisualOffset
		{
			get
			{
				float x = Mathf.Cos(_elapsed * speed * Mathf.PI) * (amplitude + RNG.Range(-1, 1) * randomness);
				return gameObject.transform.right * x;
			}
		}

		public override bool IsActive => base.IsActive && (_elapsed < duration);

		public override void Update(float dt)
		{
			base.Update(dt);

			_elapsed += dt;
		}
	}
}