using System;
using System.Collections.Generic;
using Anjin.Util;
using Combat.Entry;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Util;
using Util.Addressable;
using Util.UniTween.Value;

namespace Menu.Sticker
{
	/// <summary>
	/// A dynamic grid that can display stickers on it.
	/// </summary>
	public class StickerGrid : DynamicGrid, IRecyclable
	{
		public delegate void GridEvent(params object[] args);
		public GridEvent OnGridLockToggled;

		[Title("References")]
		public GameObject StickerPrefab;
		public Image IMG_Background;
		public Image Portrait;
		public GameObject LockedBorder;
		public GameObject Selector;
		public RectTransform CellReticle;
		[FormerlySerializedAs("CanvasGroup")]
		public CanvasGroup GridGroup;

		[Title("Configuration")]
		[FormerlySerializedAs("BackgroundSpriteInactive")]
		public Sprite SpriteBackgroundInactive;
		[FormerlySerializedAs("BackgroundSpriteActive")]
		public Sprite SpriteBackgroundActive;
		[FormerlySerializedAs("CellColorUsed")]
		public Color ColorUsedCell;
		[FormerlySerializedAs("UnusedCellColor")]
		public Color ColorUnusedCell;
		public float AlphaDamping;
		[SerializeField]
		private float InactiveOpacity;

		[NonSerialized] public int				   ID;

		[NonSerialized] public Action<GridSticker> onStickerAdded;
		[NonSerialized] public Action<GridSticker> onStickerRemoved;
		[NonSerialized] public Action<GridSticker> onStickerClicked;
		[NonSerialized] public Action              onCellsRedrawn;
		[NonSerialized] public Action<int>		   onGridSelected;

		[NonSerialized] public List<GridSticker> stickers;
		[NonSerialized] public TweenableVector2  scale;

		public bool									Locked => _isLocked;

		public Vector2Int							CurrentCellSelection => _currentCellSelection;

		private bool                                _isActive;
		private bool								_isLocked;
		private Dictionary<Vector2Int, GridSticker> _stickers;

		private Vector2Int							_currentCellSelection;

		private List<Vector2Int> _tmpOverlappingCoords = new List<Vector2Int>();

		public GridSticker HoveredSticker => _stickers.ContainsKey(hoveredCell.coordinate)
			? _stickers[hoveredCell.coordinate]
			: null;

		public void MoveCellSelection(Vector2Int coord)
		{
			_currentCellSelection.x = Mathf.Clamp(_currentCellSelection.x + coord.x, 0, (Dimensions.x - 1));
			_currentCellSelection.y = Mathf.Clamp(_currentCellSelection.y + coord.y, 0, (Dimensions.y - 1));
		}

		protected override void Awake()
		{
			base.Awake();

			_currentCellSelection = Vector2Int.zero;

			stickers  = new List<GridSticker>();
			_stickers = new Dictionary<Vector2Int, GridSticker>();

			LockedBorder.SetActive(false);

			OnGridLockToggled += ProcessGridLockToggle;

			scale = Vector2.one;
		}

		public void SetState(bool active)
		{
			_isActive             = active;
			IMG_Background.sprite = active ? SpriteBackgroundActive : SpriteBackgroundInactive;
		}

		public void SetLocked(bool locked)
		{
			_isLocked = locked;
			LockedBorder.SetActive(_isLocked);
		}

		/// <summary>
		/// Check if the specified cell is taken up by a sticker.
		/// </summary>
		public bool HasStickerAt(Vector2Int coord, out GridSticker sticker)
		{
			sticker = null;
			bool result = false;

			if (_stickers.ContainsKey(coord))
			{
				sticker = _stickers[coord];
				result = true;
			}

			return result;
		}

		public bool FindFirstFreeCell(out GridCell cell)
		{
			for (var i = 0; i < Dimensions.x; i++)
			for (var j = 0; j < Dimensions.y; j++)
			{
				if (!_stickers.ContainsKey(new Vector2Int(i, j)))
				{
					cell = this[i, j];
					return true;
				}
			}

			cell = null;
			return false;
		}

		public GridSticker GetStickerAt(Vector2Int coord)
		{
			if (_stickers.TryGetValue(coord, out GridSticker value))
				return value;

			return null;
		}

		public void AddSticker(GridSticker sticker)
		{
			Debug.Log("Add Sticker " + sticker);

			stickers.Add(sticker);

			sticker.transform.SetParent(ContentRoot, false);
			ApplyStickerLayout(sticker);

			sticker.onPointerClick = stck => onStickerClicked?.Invoke(stck);
			onStickerAdded?.Invoke(sticker);

			RefreshCells();
		}

		public void RemoveSticker(GridSticker sticker)
		{
			if (stickers.Remove(sticker))
			{
				RefreshCells();
				onStickerRemoved?.Invoke(sticker);
			}
		}

		public void ApplyStickerLayout(GridSticker sticker)
		{
			// Position the sticker on the grid.
			if (sticker.coordinate.HasValue)
			{
				Vector2 cellPosition = GetCellPosition(sticker.coordinate.Value);
				sticker.transform.anchoredPosition = cellPosition - CellSize / 2f * MathUtil.YDOWN_TO_UP;
			}
		}

		public List<Vector2Int> GetOverlappingCoords(GridSticker sticker)
		{
			bool[,] RotateArray2D(bool[,] src)
			{
				int w = src.GetLength(0);
				int h = src.GetLength(1);

				var dst = new bool[h, w];

				var ny = 0;
				for (int ox = h - 1; ox >= 0; ox--)
				{
					var nx = 0;
					for (var oy = 0; oy < w; oy++)
					{
						dst[ox, oy] = src[nx, ny];
						nx++;
					}

					ny++;
				}

				return dst;
			}

			_tmpOverlappingCoords.Clear();
			if (!sticker.coordinate.HasValue)
				return _tmpOverlappingCoords;

			// Apply rotation to the shape
			bool[,] shape = (sticker.asset ? sticker.asset : sticker.instance.Asset).shape;
			for (var i = 0; i < sticker.rotation; i++)
			{
				shape = RotateArray2D(shape);
			}

			for (var i = 0; i < shape.GetLength(0); i++)
			for (var j = 0; j < shape.GetLength(1); j++)
			{
				if (shape[i, j])
				{
					Vector2Int coord = sticker.coordinate.Value + new Vector2Int(i, j);
					_tmpOverlappingCoords.Add(coord);
				}
			}

			return _tmpOverlappingCoords;
		}

		/// <summary>
		/// Re-compute the used cells in the grid and refresh the styling.
		/// </summary>
		public void RefreshCells()
		{
			// Compute the used cells.
			// ----------------------------------------
			_stickers.Clear();

			foreach (GridSticker sticker in stickers)
			{
				foreach (Vector2Int coord in GetOverlappingCoords(sticker))
				{
					_stickers[coord] = sticker;
				}
			}


			// Update the styling.
			// ----------------------------------------
			for (var i = 0; i < Dimensions.x; i++)
			for (var j = 0; j < Dimensions.y; j++)
			{
				var      coord = new Vector2Int(i, j);
				GridCell cell  = this[coord];

				cell.Image.color = _stickers.ContainsKey(coord)
					? ColorUsedCell
					: ColorUnusedCell;
			}

			onCellsRedrawn?.Invoke();
		}

		private void LateUpdate()
		{
			transform.localScale = new Vector3(scale.value.x, scale.value.y, 1);
			GridGroup.alpha      = GridGroup.alpha.LerpDamp(_isActive ? 1 : InactiveOpacity, AlphaDamping);
		}

		public void Recycle()
		{
			onStickerAdded   = null;
			onStickerRemoved = null;
		}

		public void SelectGrid()
		{
			onGridSelected?.Invoke(ID);
		}

		public async UniTask SetPortrait(CharacterAsset asset)
		{
			var characterArt = await Addressables2.LoadHandleAsync(asset.Art);

			if (characterArt.Result != null)
			{
				Portrait.sprite = characterArt.Result;
				Portrait.rectTransform.anchoredPosition = asset.StickerMenuPosition; //(inLimbMenu ? asset.SpliceMenuPosition : asset.StickerMenuPosition);
				Portrait.transform.localScale = asset.StickerMenuScale; //(inLimbMenu ? asset.SpliceMenuScale : asset.StickerMenuScale);
				Portrait.gameObject.SetActive(true);
			}
		}

		private void ProcessGridLockToggle(params object[] args)
		{
			bool lockOccurred = (bool)args[0];

			Selector.SetActive(!lockOccurred);
		}

		private void OnDestroy()
		{
			OnGridLockToggled -= ProcessGridLockToggle;
		}
	}
}