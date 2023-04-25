using UnityEngine;

namespace Combat.UI
{
	public class AstraDamageNumber : TexturedDamageNumber
	{
		[Range(0f, 1f)]
		public float shaderIntensity;

		[Range(0f, 1f)]
		public float opacity = 1f;

		private static readonly int Opacity  = Shader.PropertyToID("_Opacity");
		private static readonly int Strength = Shader.PropertyToID("_Strength");

		public override void SetDamageNumber(int damage)
		{
			base.SetDamageNumber(damage);

			int              digits    = damage.ToString().Length;
			ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
			foreach (ParticleSystem particle in particles)
			{
				ParticleSystem.ShapeModule shape = particle.shape;
				shape.scale = new Vector3(digits / 2f, shape.scale.y, shape.scale.z);
			}
		}

		// Update is called once per frame
		private void Update()
		{
			SetShaderParameters();
		}

		// public override void OnValidate()
		// {
		// 	base.OnValidate();
		// 	SetShaderParameters();
		// }

		private void SetShaderParameters()
		{
			if (Application.isPlaying)
			{
				numberView.material.SetFloat(Strength, shaderIntensity);
				numberView.material.SetFloat(Opacity, opacity);
			}
			else
			{
				numberView.sharedMaterial.SetFloat(Strength, shaderIntensity);
				numberView.sharedMaterial.SetFloat(Opacity, opacity);
			}
		}
	}
}