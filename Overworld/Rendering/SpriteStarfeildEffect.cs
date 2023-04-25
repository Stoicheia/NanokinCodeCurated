using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Overworld.Rendering
{
	public class SpriteStarfeildEffect : SerializedMonoBehaviour
	{
		public const int MAX_COLORS = 8;

		public ColorReplacementProfile Profile;
		[Range(0f, 1f)]
		public float ReplaceRange = 0.01f;


		private SpriteRenderer rend;

		private static readonly int _propStarReplacementRange = Shader.PropertyToID("_StarReplacementRange");
		private static readonly int _propStarColorCount       = Shader.PropertyToID("_StarColorCount");
		private static readonly int _propStarReplaceColors    = Shader.PropertyToID("_StarReplaceColors");
		private static readonly int _propStarReplaceBlends    = Shader.PropertyToID("_StarReplaceBlends");

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
				rend.material.EnableKeyword("_EFFECT_STARFIELD");

				List<Vector4> fromColors = new List<Vector4>();
				List<float>   blends     = new List<float>();
				int           totalCount = 0;

				if (Profile != null)
				{
					for (int j = 0; j < Profile.colArray.Count && j < MAX_COLORS; j++)
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

						blends.Add(col.blend);

						totalCount++;
					}
				}

				var properties = new MaterialPropertyBlock();

				rend.GetPropertyBlock(properties);

				properties.SetFloat(_propStarReplacementRange, ReplaceRange);
				properties.SetInt(_propStarColorCount, totalCount);
				properties.SetVectorArray(_propStarReplaceColors, fromColors);
				properties.SetFloatArray(_propStarReplaceBlends, blends);

				rend.SetPropertyBlock(properties);
			}
		}
	}
}