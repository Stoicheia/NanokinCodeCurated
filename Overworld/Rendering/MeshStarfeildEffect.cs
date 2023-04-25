using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Overworld.Rendering
{
	public class MeshStarfeildEffect : SerializedMonoBehaviour
	{
		public const int MAX_COLORS = 8;

		public           ColorReplacementProfile Profile;
		[Range01] public float                   ReplaceRange = 0.01f;

		private MeshRenderer rend;

		public void Awake()
		{
			rend = GetComponent<MeshRenderer>();
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

				properties.SetFloat("_StarReplacementRange", ReplaceRange);
				properties.SetInt("_StarColorCount", totalCount);
				properties.SetVectorArray("_StarReplaceColors", fromColors);
				properties.SetFloatArray("_StarReplaceBlends", blends);

				rend.SetPropertyBlock(properties);
			}
		}
	}
}