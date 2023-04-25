using Sirenix.OdinInspector;
using UnityEngine;

namespace Overworld.Rendering
{
	public class TextureKeyingSetter : SerializedMonoBehaviour
	{
		private SpriteRenderer rend;
		public  Texture2D      KeyingTexture;
		public  Color          KeyingColor;

		[Range(0f, 1f)]
		public float ReplaceRange = 0.01f;

		public void Awake()
		{
			rend = GetComponent<SpriteRenderer>();
			UpdateMaterialProperties();
		}

		[Button, HideInEditorMode]
		public void UpdateMaterialProperties()
		{
			if (rend)
			{
				rend.material.EnableKeyword("_TEXTURE_KEYING");

				/*List<Vector4> fromColors = new List<Vector4>();

				int totalCount = 0;

				if(Profile != null)
				{
					for (int j = 0; j < Profile.colArray.Count; j++)
					{
						var col = Profile.colArray[j];
						if (QualitySettings.activeColorSpace == ColorSpace.Linear)
						{
							fromColors.Add(col.from.linear);
							//toColors.Add(col.to.linear);
						}
						else
						{
							fromColors.Add(col.from);
							//toColors.Add(col.to);
						}

						totalCount++;
					}
				}*/

				var properties = new MaterialPropertyBlock();

				rend.GetPropertyBlock(properties);

				properties.SetFloat("_KeyRange",     ReplaceRange);
				properties.SetColor("_KeyFromColor", KeyingColor);
				//properties.SetTexture("_KeyingTex",  KeyingTexture);

				rend.SetPropertyBlock(properties);
			}
		}
	}
}