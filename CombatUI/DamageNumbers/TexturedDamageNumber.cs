using UnityEngine;

namespace Combat.UI
{
	public class TexturedDamageNumber : DamageNumber
	{
		public MeshRenderer numberView;
		public Material     viewMaterial;
		public Camera       numberCamera;

		private                 RenderTexture _renderTexture;
		private static readonly int           RenderTexture = Shader.PropertyToID("_RenderTexture");

		private const int TextureWidth       = 512;
		private const int TextureHeight      = 256;
		private const int TextureBufferDepth = 24; // 0, 16, or 24

		public virtual void Awake()
		{
			UpdateTextureTargets();
		}

		private void UpdateTextureTargets()
		{
			if (!_renderTexture)
			{
				_renderTexture = new RenderTexture(TextureWidth, TextureHeight, TextureBufferDepth);
			}

			if (numberView != null)
			{
				numberView.material = new Material(viewMaterial);

				if (Application.isPlaying)
				{
					numberView.material.SetTexture(RenderTexture, _renderTexture);
				}
				else
				{
					numberView.sharedMaterial.SetTexture(RenderTexture, _renderTexture);
				}
			}

			if (numberCamera != null)
			{
				numberCamera.targetTexture = _renderTexture;
			}
		}

		// public override void OnValidate()
		// {
		// 	base.OnValidate();
		// 	UpdateTextureTargets();
		// }
	}
}