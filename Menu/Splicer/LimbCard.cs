using System;
using Anjin.Util;
using API.Spritesheet.Indexing;
using Assets.Nanokins;
using Cysharp.Threading.Tasks;
using Data.Nanokin;
using Puppets;
using Sirenix.OdinInspector;
using TMPro;
using UnityEditor.AI.Anjin;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Util;
using Util.Addressable;

/// <summary>
/// A single cell in the splicer's limb grid.
/// </summary>
public class LimbCard : SelectableExtended<LimbCard>, IRecyclable
{
	[Title("References")]
	[SerializeField] private TextMeshProUGUI TMP_Name;
	[SerializeField] private Image IMG_Limb;
	[SerializeField] private Image IMG_Frame;
	[SerializeField] private Image IMG_Mask;
	[SerializeField] private Image IMG_FavoriteIcon;

	[Title("Setup"), FormerlySerializedAs("RenderParamsEquipped")]
	[SerializeField] private StyleParameters EquippedStyling;
	[SerializeField, FormerlySerializedAs("RenderParamsUnequipped")]
	private StyleParameters UnequippedStyling;

	// OLD
	[SerializeField] private Sprite InactiveImage;
	[SerializeField] private Sprite InactiveMaskImage;
	[SerializeField] private float  InactiveScale = 1;
	[Space]
	[SerializeField] private Sprite ActiveImage;
	[SerializeField] private Sprite ActiveMaskImage;
	[SerializeField] private float  ActiveScale = 1.25f;

	[Serializable]
	public struct StyleParameters
	{
		[SerializeField] public Sprite Frame;
		[SerializeField] public Sprite FrameMask;
		[SerializeField] public float  Scale;
	}

	// Runtime stuff
	// ----------------------------------------
	[NonSerialized] public LimbInstance  limb;
	[NonSerialized] public RectTransform rect;

	public bool Favorited { get { return ((IMG_FavoriteIcon != null) ? IMG_FavoriteIcon.gameObject.activeSelf : false); } }

	private LimbState _state;

	private AsyncOperationHandle<IndexedSpritesheetAsset> _spritesheetHandle;

	protected override LimbCard Myself => this;

	protected override void Awake()
	{
		base.Awake();

		rect           = GetComponent<RectTransform>();
		IMG_Limb.color = Color.clear;

		SetNone();
	}

	private void SetNone()
	{
		limb            = null;
		IMG_Limb.sprite = null;
		IMG_Limb.color  = Color.clear;
		IMG_FavoriteIcon.gameObject.SetActive(false);
		gameObject.name = "NULL";

		SetEquipped(false);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		Addressables2.ReleaseSafe(_spritesheetHandle);
	}

	public async UniTask SetLimb(LimbEntry newLimb)
	{
		if (newLimb == null)
		{
			SetNone();
			return;
		}

		limb = newLimb.instance;
		NanokinLimbAsset asset = limb.Asset;

		Addressables2.ReleaseSafe(_spritesheetHandle);
		Addressables2.ReleaseSafe(asset.Spritesheet.OperationHandle);
		if(!asset.Spritesheet.OperationHandle.IsValid())
			_spritesheetHandle = asset.Spritesheet.LoadAssetAsync<IndexedSpritesheetAsset>();
		else
			_spritesheetHandle = asset.Spritesheet.OperationHandle.Convert<IndexedSpritesheetAsset>();
		await _spritesheetHandle;

		_state = new LimbState(asset, _spritesheetHandle.Result)
		{
			cell = Vector2Int.zero
		};



		TMP_Name.color = Color.white;
		IMG_Limb.color = Color.white;

		if (_state.currentSprite != null)
		{
			IMG_Limb.sprite = _state.currentSprite;

			Rect    bounds = SpriteTrimmingCache.GetTrimmedBounds(_state.currentSprite);
			Vector2 center = bounds.center - _state.currentSprite.size() / 2;

			IMG_Limb.rectTransform.anchoredPosition = new Vector2(-center.x, center.y) * IMG_Limb.rectTransform.localScale.xy();
			IMG_Limb.rectTransform.sizeDelta        = _state.currentSprite.size();

			string limbName = asset.FullName;
			gameObject.name = $"Limb Card ({limbName})";
			TMP_Name.text   = limbName;

			IMG_FavoriteIcon.gameObject.SetActive(limb.Favorited);
		}
		else
		{
			TMP_Name.text   = "ERROR";
			IMG_Limb.sprite = null;
			IMG_Limb.color  = Color.red;
			IMG_FavoriteIcon.gameObject.SetActive(false);
		}
	}

	/// <summary>
	/// Changes the styling to the equipped state.
	/// </summary>
	/// <param name="state"></param>
	public void SetEquipped(bool state)
	{
		StyleParameters rp = state ? EquippedStyling : UnequippedStyling;

		IMG_Frame.sprite = rp.Frame;
		rect.sizeDelta   = rp.Frame.size();
		rect.localScale  = rp.Scale * Vector3.one;
	}

	/// <summary>
	/// Changes the visibility of the favorite icon depending on whether or not this limb has been favorited.
	/// </summary>
	/// <param name="state"></param>
	public void SetFavorited(bool state)
	{
		IMG_FavoriteIcon.gameObject.SetActive(state);

		if (limb != null)
		{
			limb.Favorited = state;
		}
	}

	public override void Recycle()
	{
		base.Recycle();
		SetNone();
	}
}