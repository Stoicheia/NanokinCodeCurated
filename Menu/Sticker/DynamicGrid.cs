using System;
using Anjin.Util;
using System.Collections.Generic;
using Anjin.EditorUtility;
using Overworld.Controllers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.UI;
using Vexe.Runtime.Extensions;

namespace Menu.Sticker
{
	public class DynamicGrid : MonoBehaviour
	{
		[Title("References")]
		public GameObject CellPrefab;
		public Transform CellRoot;
		public Transform ContentRoot;

		[Title("Layout")]
		[Title("Grid", HorizontalLine = false)]
		[OnValueChanged("OnLayoutChanged")] public Vector2Int Dimensions = new Vector2Int(5, 4);
		[OnValueChanged("OnLayoutChanged")] public Vector2 Margin = new Vector2(4, 4);
		[Title("Cell", HorizontalLine = false)]
		[OnValueChanged("OnLayoutChanged")] public int CellPadding = 4;
		[OnValueChanged("OnLayoutChanged")] public int     CellSize   = 48;
		[OnValueChanged("OnLayoutChanged")] public Vector2 CellOffset = Vector2.zero;

		[NonSerialized] public     CanvasGroup   CellGroup;
		[NonSerialized] public     CanvasGroup   ContentGroup;
		[NonSerialized] public new RectTransform transform;
		[NonSerialized] public     GridCell      hoveredCell;

		[NonSerialized] public Action<GridCell> onCellHoverChanged;
		[NonSerialized] public Action<GridCell> onCellSelected;

		private Dictionary<Vector2Int, GridCell> _cells;

		private Vector2Int _currentDimensions;

		private Vector2 TotalSize => Dimensions * CellSize + Dimensions * CellPadding;

		public GridCell this[Vector2Int coord] => _cells[coord];

		public GridCell this[int i, int j] => _cells[new Vector2Int(i, j)];

		protected virtual void Awake()
		{
			transform = GetComponent<RectTransform>();
			_cells    = new Dictionary<Vector2Int, GridCell>();


			if (CellRoot == null)
			{
				CellRoot = ((Component) this).transform;
			}

			CellGroup    = CellRoot.GetOrAddComponent<CanvasGroup>();
			ContentGroup = ContentRoot.GetOrAddComponent<CanvasGroup>();

			RefreshLayout();
		}

		/// <summary>
		/// Check if the coordinate is out of the grid's bounds.
		/// </summary>
		public bool IsOutOfBounds(Vector2Int coord) => !OtherExtensions.Between(coord.x, 0, Dimensions.x - 1) || !OtherExtensions.Between(coord.y, 0, Dimensions.y - 1);

		/// <summary>
		/// Get the anchorPos for a cell at the specified coordinate.
		/// </summary>
		public Vector2 GetCellPosition(Vector2 coord)
		{
			// Flip the Y axis so (0,0) is at the top-left.
			// That way, our cell coordinates work like a standard multi-dimensional array.
			coord.y = Dimensions.y - 1 - coord.y;

			return TotalSize * -.5f               // Pivot of the grid is at the center
			       + CellSize * .5f * Vector2.one // Pivot is at the center of the cell (0.5,0.5)
			       + coord * CellSize
			       + coord * CellPadding
			       + CellOffset;
		}

		/// <summary>
		/// Refresh the layout based on layout options and requested grid dimensions.
		/// All necessary prefabs and cells will be instantiated.
		/// </summary>
		public void RefreshLayout()
		{
			// Delete the old cells (we could also update existing ones that are still valid)
			// ----------------------------------------
			if (_currentDimensions != Dimensions)
			{
				DeleteCells();
			}

			_currentDimensions = Dimensions;

			// Update the grid itself.
			// ----------------------------------------
			transform.sizeDelta = TotalSize + Margin * 2; // multiply by 2 to cover both sides.


			// Create and update used cells.
			// ----------------------------------------
			for (var i = 0; i < Dimensions.x; i++)
			for (var j = 0; j < Dimensions.y; j++)
			{
				var coord = new Vector2Int(i, j);

				if (!_cells.TryGetValue(coord, out GridCell cell))
				{
					// Create the cell
					// ----------------------------------------

					GameObject goCell = PrefabPool.Rent(CellPrefab, CellRoot);
					goCell.name      = $"Cell ({i}, {j})";
					goCell.hideFlags = HideFlags.DontSaveInEditor;
					cell             = goCell.GetOrAddComponent<GridCell>();

					cell.onPointerEnter = OnCellPointerEnter;
					cell.onPointerExit  = OnCellPointerExit;
					cell.onSelected     = OnCellSelected;

					_cells.Add(coord, cell);
				}

				cell.transform.anchoredPosition = GetCellPosition(coord);
				cell.coordinate                 = coord;
			}

			// Update navigation
			// ----------------------------------------
			// Need to do this after the fact otherwise
			// some neighbors won't yet exist for the
			// current cell being prepared
			for (var i = 0; i < Dimensions.x; i++)
			for (var j = 0; j < Dimensions.y; j++)
			{
				GridCell cell = this[i, j];

				Navigation nav = cell.ToExplicit();

				if (i == 0) nav.selectOnLeft                 = null;
				if (i == Dimensions.x - 1) nav.selectOnRight = null;
				if (j == 0) nav.selectOnUp                   = null;
				if (j == Dimensions.y - 1) nav.selectOnDown  = null;

				cell.navigation = nav;
			}
		}

		private void OnCellSelected(GridCell cell)
		{
			onCellSelected?.Invoke(cell);
		}

		private void OnCellPointerExit(GridCell cell)
		{
			if (hoveredCell == cell)
			{
				hoveredCell = null;
				onCellHoverChanged?.Invoke(hoveredCell);
			}
		}

		private void OnCellPointerEnter(GridCell cell)
		{
			hoveredCell = cell;
			onCellHoverChanged?.Invoke(hoveredCell);
		}

		private void OnDestroy()
		{
			DeleteCells();
		}

		private void DeleteCells()
		{
			if (_cells == null) return;

			foreach (GridCell cell in _cells.Values)
			{
				PrefabPool.DestroyOrReturn(cell.gameObject);
			}

			_cells.Clear();
		}

#if UNITY_EDITOR
		// ReSharper disable once UnusedMember.Local
		private void OnLayoutChanged()
		{
			if (Application.isPlaying)
			{
				RefreshLayout();
			}
		}
#endif
	}
}