using System;
using System.Collections.Generic;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using SaveFiles;
using SaveFiles.Elements.Inventory.Items;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Util;
using Util.Addressable;

namespace Menu.Sticker
{
	/// <summary>
	/// A sticker being shown on a sticker grid.
	/// </summary>
	public class GridSticker : SelectableExtended<GridSticker>, IRecyclable
	{
		public bool withinGrid;
		[SerializeField] public Image Image;

		[NonSerialized] public     StickerAsset        asset;
		[NonSerialized] public     StickerInstance instance;
		[NonSerialized] public new RectTransform       transform;
		[NonSerialized] public     int                 cellSize;

		[NonSerialized] public Vector2Int? coordinate;
		[NonSerialized] public int         rotation;

		private AsyncOperationHandle<Sprite> _spriteHandle;

		private Vector2 correctedSize;

		private Vector2 anchorCorrection;

		protected override GridSticker Myself => this;

		protected override void Awake()
		{
			base.Awake();
			transform = GetComponent<RectTransform>();
			correctedSize = Vector2.zero;
			anchorCorrection = new Vector2(1, -1);
			SetNone();
			withinGrid = false;
		}

		public void SetNone()
		{
			asset                      = null;
			Image.sprite               = null;
			Image.color                = Color.clear;
			transform.localEulerAngles = Vector3.zero;
		}

		public async UniTask SetSticker([NotNull] StickerAsset asset)
		{
			Addressables2.ReleaseSafe(_spriteHandle);

			this.asset = asset;

			_spriteHandle = await Addressables2.LoadHandleAsync<Sprite>(asset.Sprite);
			Image.sprite  = _spriteHandle.Result;

			Display();
		}

		public async UniTask SetSticker(StickerInstance mirror)
		{
			asset       = mirror.Asset;
			this.instance = mirror;

			_spriteHandle = await Addressables2.LoadHandleAsync<Sprite>(asset.Sprite);
			Image.sprite  = _spriteHandle.Result;

			Display();
		}

		public void Rotate(int change)
		{
			rotation += change;
			rotation =  rotation.Wrap(4);

			Display();
		}

		private void Display()
		{
			if (Image.sprite == null)
			{
				Image.color                = Color.white;
				transform.localEulerAngles = Vector3.zero;
				return;
			}

			Image.color = Color.white;

			// Size correction ------------------------------
			Vector2 spriteSize    = Image.sprite.size();
			Vector2 referenceSize = cellSize * asset.Dimensions;

			float   ratio         = Mathf.Min(referenceSize.x / spriteSize.x, referenceSize.y / spriteSize.y);
			correctedSize = spriteSize * ratio * asset.UIScale;
			Image.rectTransform.sizeDelta = correctedSize;

			// Rotation ------------------------------
			Image.rectTransform.localEulerAngles = new Vector3(0, 0, -90 * rotation);

			//if (rotation == 0 || rotation == 2)
			//{
			//	anchorCorrection.x = 1;
			//	anchorCorrection.y = -1;
			//}
			//else
			//{
			//	anchorCorrection.x = -1;
			//	anchorCorrection.y = 1;
			//}

			// Offset correction for center pivot ------------------------------
			Image.rectTransform.anchoredPosition = (withinGrid ? (correctedSize / 2f * anchorCorrection) : Vector2.zero);
		}

		public void MaintainImagePosition()
		{
			Image.rectTransform.anchoredPosition = (withinGrid ? (correctedSize / 2f * anchorCorrection) : Vector2.zero);
		}

		public void Recycle()
		{
			SetNone();
		}
	}
}