using System;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using Util.Extensions;

namespace Util.RenderingElements
{
	public class SetRenderTextureOnStart : SerializedMonoBehaviour
	{
		[OdinSerialize, NonSerialized]
		public AssetReferenceT<RenderTexture> RenderTexture;

		private async void Awake()
		{
			RenderTexture rt = await RenderTexture.LoadAssetAsync();

			if (rt == null)
			{
				this.LogError($"Couldnt load RenderTexture at address {RenderTexture.RuntimeKey}.");
				return;
			}

			if (gameObject.TryGetComponent(out Camera cam))
			{
				cam.targetTexture = rt;
			}

			if (gameObject.TryGetComponent(out RawImage img))
			{
				img.texture = rt;
			}
		}

		private void OnDestroy()
		{
			RenderTexture.ReleaseAsset();
		}
	}
}