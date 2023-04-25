using UnityEngine;

namespace Combat.UI
{
	public class PierceDamageNumber : TexturedDamageNumber
	{
		[Range(0f, 1.5f)]
		public float animationTime;

		private static readonly int AnimationTime = Shader.PropertyToID("_AnimationTime015");

		// Update is called once per frame
		private void Update()
		{
			if (Application.isPlaying)
			{
				numberView.material.SetFloat(AnimationTime, animationTime);
			}
			else
			{
				numberView.sharedMaterial.SetFloat(AnimationTime, animationTime);
			}
		}
	}
}