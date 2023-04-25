using System;
using Cysharp.Threading.Tasks;
using SaveFiles;
using SaveFiles.Elements.Inventory.Items;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using Util;
using Util.Addressable;

namespace Menu.Sticker
{
	public class ListSticker : SelectableExtended<ListSticker>, IRecyclable
	{
		public GameObject	   FavoriteIcon;
		public Image           Image;
		public TextMeshProUGUI Label;
		public TextMeshProUGUI Charges;

		[NonSerialized] public StickerAsset        asset;
		[NonSerialized] public StickerEntry        entry;
		[NonSerialized] public StickerInstance instance;

		private AsyncOperationHandle<Sprite> _spriteHandle;

		public bool Favorited { get { return ((FavoriteIcon != null) ? FavoriteIcon.activeSelf : false); } }

		protected override ListSticker Myself => this;

		public void SetNone()
		{
			if (asset != null)
				Addressables2.Release(_spriteHandle);

			Image.color  = Color.clear;
			Image.sprite = null;
			Label.text   = "";
			FavoriteIcon.SetActive(false);
		}

		protected override void Awake()
		{
			base.Awake();

			SetNone();
		}

		/*public async UniTask SetSticker(StickerAsset value)
		{
			asset = value;

			Image.color     = Color.white;
			Label.text      = asset.DisplayName;
			gameObject.name = asset.DisplayName;

			_spriteHandle = await Addressables2.LoadHandleAsync<Sprite>(asset.Sprite);
			Image.sprite  = _spriteHandle.Result;
		}*/

		public async UniTask SetSticker(StickerInstance instance)
		{
			asset           = instance.Asset;
			entry           = instance.Entry;
			this.instance   = instance;

			gameObject.name = asset.DisplayName;

			_spriteHandle = await Addressables2.LoadHandleAsync(asset.Sprite);
			Image.sprite  = _spriteHandle.Result;

			FavoriteIcon.gameObject.SetActive(entry.Favorited);

			RefreshUI();
		}

		public void RefreshUI()
		{
			Image.color  = Color.white;
			Label.text   = asset.DisplayName;
			//Charges.text = instance.Charges.ToString();
		}

		public void SetFavorited(bool state)
		{
			FavoriteIcon.gameObject.SetActive(state);

			if (instance != null)
			{
				instance.Favorited = state;
			}
		}

		public override void Recycle()
		{
			base.Recycle();
			SetNone();
		}
	}
}