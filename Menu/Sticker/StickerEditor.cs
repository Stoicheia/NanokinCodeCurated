using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.EditorUtility;
using Anjin.Nanokin;
using JetBrains.Annotations;
using Overworld.Controllers;
using Pathfinding.Util;
using SaveFiles;
using SaveFiles.Elements.Inventory.Items;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.InputSystem;
using Util.Extensions;

namespace Menu.Sticker
{
	/// <summary>
	/// The component responsible for placing stickers and modifying placements on a StickerGrid.
	/// Handles inputs and other functionalities of placement.
	/// </summary>
	public class StickerEditor : SerializedMonoBehaviour
	{
		[Title("Setup")]
		[SerializeField, SceneObjectsOnly] private Canvas Canvas;
		[SerializeField] private Vector2 StickerPlacingPivot;
		[SerializeField] private Vector2 StickerPlacingAnchorMin;
		[SerializeField] private Vector2 StickerPlacingAnchorMax;
		[SerializeField] private float   StickerPlacingScale = 1.2f;

		[Title("Sounds")]
		[SerializeField] private AudioDef AdPlacementRotateLeft;
		[SerializeField] private AudioDef AdPlacementRotateRight;
		[SerializeField] private AudioDef AdPlacementGridSnap;
		[SerializeField] private AudioDef AdPlacementCancel;
		[SerializeField] private AudioDef AdPlacementSuccess;
		[SerializeField] private AudioDef AdPlacementFail;
		[SerializeField] private AudioDef AdDetachFromGrid;

		[Title("Appearance")]
		[SerializeField] private Color CellPlacementValidColor;
		[SerializeField] private Color CellPlacementInvalidColor;

		private GridSticker _activePlacement;

		private GridCell _lastHoveredCell;
		private Vector2  _stickerAnchorForGrid;
		private Vector2  _stickerPivotForGrid;
		private Vector2  _stickerScaleForGrid;

		private StickerGrid _grid;

		public bool IsPlacing => _activePlacement != null;

		public bool GridLocked => ((_grid != null) ? _grid.Locked : false);

		public Vector2Int CurrentCellSelection => _grid.CurrentCellSelection;

		public  Action<GridSticker> onStickerDetached;
		public  Action<GridSticker> onPlacementConfirmed;
		public  Action<GridSticker> onPlacementCanceled;
		private bool                _disableInteractionsTemporarily; // This is used so that the

		private RectTransform Reticle => _grid.CellReticle;

		public GridCell ActiveCell { get; private set; }

		public void ToggleGridLock(bool locked = false)
		{
			if (_grid != null)
			{
				_grid.SetLocked(locked);
				StickerMenu.Live.ToggleGridSelectors();
			}
		}

		public bool HasStickerAt(Vector2Int coord, out GridSticker sticker)
		{
			bool result = false;
			sticker = null;

			if (_grid != null)
			{
				result = _grid.HasStickerAt(coord, out sticker);
			}

			return result;
		}

		public void MoveCellSelection(Vector2Int coord)
		{
			_grid.MoveCellSelection(coord);
			Reticle.anchoredPosition = _grid.GetCellPosition(CurrentCellSelection);
		}

		public bool FindFirstFreeCell(out GridCell cell)
		{
			bool result = false;
			cell = null;

			if (_grid != null)
			{
				result = _grid.FindFirstFreeCell(out cell);
			}

			return result;
		}

		/// <summary>
		/// Change the grid being edited.
		/// </summary>
		public void SetGrid(StickerGrid grid)
		{
			if (_grid != null)
			{
				_grid.ContentGroup.blocksRaycasts = false;
				_grid.CellGroup.blocksRaycasts    = false;

				_grid.onCellsRedrawn -= OnCellsRedrawn;
			}

			_grid = grid;

			if (_grid != null)
			{
				_grid.ContentGroup.blocksRaycasts = true;
				_grid.CellGroup.blocksRaycasts    = false;

				_grid.onStickerClicked = null;

				_grid.onCellsRedrawn     += OnCellsRedrawn;
				_grid.onStickerClicked   += OnStickerClicked;
				_grid.onCellHoverChanged += OnCellHoverChanged;
				_grid.onCellSelected     += OnCellSelected;

				RectTransform stickerRect = _grid.StickerPrefab.GetComponent<RectTransform>();

				_stickerPivotForGrid  = stickerRect.pivot;
				_stickerAnchorForGrid = stickerRect.anchorMin;
				_stickerScaleForGrid  = stickerRect.localScale;
			}
		}

		private void OnCellSelected(GridCell cell)
		{
			if (IsPlacing)
			{
				MovePlacementToCell(cell);
			}
		}

		private void OnCellHoverChanged(GridCell cell)
		{
			if (cell != null)
			{
				ActiveCell = cell;

				if (Reticle.gameObject.activeSelf)
				{
					MoveCellSelection(cell.coordinate);
				}

				//CurrentCellSelection = cell.coordinate;
				//UGUI.Select(cell);
			}
		}

		private void OnStickerClicked(GridSticker sticker)
		{
			StartPlacementByDetach(sticker);
		}

		private void OnStartPlacing(GridSticker sticker)
		{
			// Make it so we are now choosing cells instead of stickers
			_grid.ContentGroup.blocksRaycasts = false;
			_grid.CellGroup.blocksRaycasts    = true;
			_grid.CellGroup.interactable      = true;

			// Setup the sticker
			sticker.cellSize                = _grid.CellSize + _grid.CellPadding;
			_disableInteractionsTemporarily = true;
			_activePlacement                = sticker;


			_grid.RefreshCells();
		}

		private void OnStopPlacing()
		{
			_activePlacement                  = null;
			_grid.ContentGroup.blocksRaycasts = true;
			_grid.CellGroup.blocksRaycasts    = false;
			_grid.CellGroup.interactable      = false;
		}

		/// <summary>
		/// Begin placing a new sticker.
		/// </summary>
		public bool StartPlacementNew([CanBeNull] out GridSticker sticker)
		{
			if (_activePlacement != null)
			{
				// Must cancel the existing placement first.
				CancelPlacement();
			}

			if (!_grid)
			{
				this.LogError("Cannot BeginPlacement because we are not editing any grid.");
				sticker = null;
				return false;
			}

			sticker = PrefabPool.Rent<GridSticker>(_grid.StickerPrefab, Canvas.transform);

			OnStartPlacing(sticker);
			MovePlacementToMouse();

			return true;
		}

		/// <summary>
		/// Begin placing a sticker by detaching an existing one.
		/// </summary>
		/// <param name="existing"></param>
		public void StartPlacementByDetach([NotNull] GridSticker existing)
		{
			if (!existing.coordinate.HasValue)
			{
				this.LogError("Not an attached sticker.", nameof(StartPlacementByDetach));
				return;
			}

			Vector2Int coord = existing.coordinate.Value;

			if (!GridLocked)
			{
				ToggleGridLock(true);
			}

			_grid.RemoveSticker(existing);
			onStickerDetached?.Invoke(existing);

			GridCell startCell = _grid[coord];

			OnStartPlacing(existing);
			MovePlacementToCell(startCell);

			//UGUI.Select(startCell);
			ActiveCell = startCell;

			GameSFX.PlayGlobal(AdDetachFromGrid);
		}

		/// <summary>
		/// Cancel the placement in progress.
		/// </summary>
		public void CancelPlacement()
		{
			GridSticker sticker = _activePlacement;
			if (sticker == null)
			{
				Debug.LogWarning("StickerEditor: cannot cancel the placement since there is none at the present time.");
				return;
			}


			onPlacementCanceled?.Invoke(sticker);

			OnStopPlacing();

			Destroy(sticker.gameObject);
			_grid.RefreshCells();

			GameSFX.PlayGlobal(AdPlacementCancel);
		}

		/// <summary>
		/// Confirm the placement in progress. (with the coordinates already set on the sticker)
		/// </summary>
		public void ConfirmPlacement()
		{
			GridSticker sticker = _activePlacement;

			if (IsPlacementValid())
			{
				_activePlacement = null;

				_grid.AddSticker(sticker);
				onPlacementConfirmed?.Invoke(sticker);
				OnStopPlacing();

				GameSFX.PlayGlobal(AdPlacementSuccess);
			}
			else
			{
				GameSFX.PlayGlobal(AdPlacementFail);
			}
		}

		/// <summary>
		/// Rotate the sticker being placed.
		/// </summary>
		/// <param name="dir"></param>
		public void RotatePlacement(int dir)
		{
			_activePlacement.Rotate(dir);
			_grid.RefreshCells();

			if (dir < 0) GameSFX.PlayGlobal(AdPlacementRotateLeft);
			if (dir > 0) GameSFX.PlayGlobal(AdPlacementRotateRight);
		}

		public void RefreshLayout()
		{
			if (IsPlacing && _activePlacement != null)
			{
				_grid.RefreshCells();

				if (_activePlacement.coordinate.HasValue)
				{
					_activePlacement.transform.SetParent(_grid.ContentRoot, false);

					_activePlacement.transform.anchorMin = _stickerAnchorForGrid;
					_activePlacement.transform.anchorMax = _stickerAnchorForGrid;
					_activePlacement.transform.pivot = _stickerPivotForGrid;
					_activePlacement.transform.localScale = _stickerScaleForGrid;

					_grid.ApplyStickerLayout(_activePlacement);
					//_grid.ApplyStickerLayout(_activePlacement);
				}

				_grid.RefreshCells();
			}
		}

		private bool IsPlacementValid()
		{
			if (!_activePlacement.coordinate.HasValue) return false;

			List<Vector2Int> coords = _grid.GetOverlappingCoords(_activePlacement);
			return !coords.Any(coord => _grid.IsOutOfBounds(coord) || _grid.GetStickerAt(coord));
		}

		private void MovePlacementToMouse()
		{
			if (Mouse.current == null) return;
			if (_grid.hoveredCell == null)
			{
				if (_activePlacement.coordinate.HasValue)
				{
					// Unsnap from the grid
					_activePlacement.coordinate = null;
					_activePlacement.transform.SetParent(Canvas.transform, false);
					//_grid.RefreshCells();
				}

				

				_activePlacement.transform.pivot = StickerPlacingPivot;
				//_activePlacement.transform.anchorMin  = Vector2.zero;
				//_activePlacement.transform.anchorMax  = Vector2.zero;
				_activePlacement.transform.anchorMin = StickerPlacingAnchorMin;
				_activePlacement.transform.anchorMax = StickerPlacingAnchorMax;
				_activePlacement.transform.localScale = _stickerScaleForGrid * StickerPlacingScale;

				// Stick to the mouse.
				//_activePlacement.transform.anchoredPosition = Canvas.CalculatePositionFromMouseToRectTransform(Canvas.worldCamera);

				Vector3 mousePosition = Mouse.current.position.ReadValue();
				mousePosition.z = 0;

				//_activePlacement.transform.position = Canvas.CalculatePositionFromMouseToRectTransform(Canvas.worldCamera);

				Debug.Log("Mouse position: " + mousePosition + "; placement position: " + _activePlacement.transform.anchoredPosition);

				_activePlacement.transform.position = mousePosition;
				//_activePlacement.transform.anchoredPosition = Canvas.CalculatePositionFromMouseToRectTransform(Canvas.worldCamera);
				//_activePlacement.transform.anchoredPosition = Canvas.CalculatePositionFromTransformToRectTransform(Mouse.current.position.ReadValue(), Canvas.worldCamera);
				//_activePlacement.transform.anchoredPosition = Canvas.NewCalculatePositionFromMouseToRectTransform();
				//_activePlacement.transform.position = Mouse.current.position.ReadValue();

				_grid.RefreshCells();

				_activePlacement.withinGrid = false;
				_activePlacement.MaintainImagePosition();
			}
			else
			{
				MovePlacementToCell(_grid.hoveredCell);
			}
		}

		private void MovePlacementToCell([NotNull] GridCell cell)
		{
			if (_activePlacement.coordinate == cell.coordinate)
				return;

			int rotation = _activePlacement.rotation;
			int width, height;

			if (rotation == 0 || rotation == 2)
			{
				width = _activePlacement.asset.Columns;
				height = _activePlacement.asset.Rows;
			}
			else
			{
				width = _activePlacement.asset.Rows;
				height = _activePlacement.asset.Columns;
			}

			var coordinate = cell.coordinate;

			if ((coordinate.x + width) > _grid.Dimensions.x)
			{
				coordinate.x = _grid.Dimensions.x - width;
			}

			if ((coordinate.y + height) > _grid.Dimensions.y)
			{
				coordinate.y = _grid.Dimensions.y - height;
			}

			// Stick to the grid.
			_activePlacement.transform.SetParent(_grid.ContentRoot, false);
			_activePlacement.coordinate = coordinate; // TODO adjust for centering with sub-cell division for even sticker dimensions (NANO-355)

			_activePlacement.transform.anchorMin  = _stickerAnchorForGrid;
			_activePlacement.transform.anchorMax  = _stickerAnchorForGrid;
			_activePlacement.transform.pivot      = _stickerPivotForGrid;
			_activePlacement.transform.localScale = _stickerScaleForGrid;

			_grid.ApplyStickerLayout(_activePlacement);
			_grid.RefreshCells();

			_activePlacement.withinGrid = true;
			_activePlacement.MaintainImagePosition();
		}

		private void Update()
		{
			if (!_grid) return;

			// Placement interactions/inputs
			// ----------------------------------------
			if (!_disableInteractionsTemporarily)
				DoPlacementInteractions();

			_disableInteractionsTemporarily = false;


			// Update placement position.
			// ----------------------------------------
			if (IsPlacing)
			{
				if (Mouse.current != null && Mouse.current.delta.IsActuated(1f))
					MovePlacementToMouse();

				if (_grid.hoveredCell != _lastHoveredCell)
				{
					_grid.RefreshCells();
					_lastHoveredCell = _grid.hoveredCell;

					GameSFX.PlayGlobal(AdPlacementGridSnap, transform);
				}
			}
		}

		private void DoPlacementInteractions()
		{
			if (IsPlacing)
			{
				if (Mouse.current.leftButton.wasPressedThisFrame)
				{
					if (_grid.hoveredCell != null)
						ConfirmPlacement();
					else
						CancelPlacement();
				}
				else if (Mouse.current.rightButton.wasPressedThisFrame)
				{
					CancelPlacement();
				}

				if (GameInputs.menuLeft.AbsorbPress(0.3f)) RotatePlacement(-1);
				if (GameInputs.menuRight.AbsorbPress(0.3f)) RotatePlacement(1);

				if (GameInputs.confirm.IsPressed && (ActiveCell != null))
				{
					GameInputs.confirm.AbsorbPress(0.1f);
					MovePlacementToCell(ActiveCell);
					ConfirmPlacement();
				}

				if (GameInputs.cancel.IsPressed)
				{
					GameInputs.cancel.AbsorbPress(0.1f);
					CancelPlacement();
				}

				if (GameInputs.menuNavigate.AnyPressed && _activePlacement.coordinate.HasValue)
				{
					Vector2Int movement = Vector2Int.RoundToInt(GameInputs.menuNavigate.Value);
					movement.y *= -1;

					MoveCellSelection(movement);
					ActiveCell = _grid[CurrentCellSelection];

					MovePlacementToCell(ActiveCell);
				}
			}
			else
			{
				if (GameInputs.confirm.IsPressed && HasStickerAt(CurrentCellSelection, out GridSticker gsticker))
				{
					GameInputs.confirm.AbsorbPress(0.1f);
					StartPlacementByDetach(gsticker);
				}

				// TODO remove, no longer needed since we can simply click stickers directly now
				// if (Mouse.current.leftButton.wasPressedThisFrame && _grid.hoveredCell)
				// {
				// 	// Detach Hovered Sticker
				// 	// ----------------------------------------
				// 	if (_grid.HoveredSticker != null)
				// 	{
				// 		BeginPlacement(_grid.HoveredSticker);
				// 	}
				// }
			}
		}

		private void OnCellsRedrawn()
		{
			if (!IsPlacing) return;

			// Cells already with a sticker on them will be colored
			// red to color-code the 'collision'
			List<Vector2Int> overlappingCoords = _grid.GetOverlappingCoords(_activePlacement);

			foreach (Vector2Int overlapCoord in overlappingCoords)
			{
				if (!_grid.IsOutOfBounds(overlapCoord))
				{
					_grid[overlapCoord].Image.color = (!HasStickerAt(overlapCoord, out _) ? CellPlacementValidColor : CellPlacementInvalidColor);
				}
			}
		}

		public void Select(GridCell cell)
		{
			if (!IsPlacing) return;

			ActiveCell = cell;
			MovePlacementToCell(cell);
		}
	}
}