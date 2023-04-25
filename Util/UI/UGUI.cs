using System;
using System.Collections.Generic;
using Anjin.Util;
using JetBrains.Annotations;
using Pathfinding.Util;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Util;

namespace Anjin.EditorUtility
{
	public static class UGUI
	{
		public static GameObject SelectedObject => EventSystem.current.currentSelectedGameObject;

		private static Vector3[] _tmpCorners = new Vector3[4];

		public static bool GetSelectedAny<TSelectable>(out TSelectable sel)
		{
			if (SelectedObject != null && SelectedObject.TryGetComponent(out sel))
				return true;

			sel = default;
			return false;
		}

		public static bool GetSelected<TSelectable>(out TSelectable sel)
			where TSelectable : Selectable
		{
			if (SelectedObject != null && SelectedObject.TryGetComponent(out sel))
				return true;

			sel = null;
			return false;
		}

		public static void SelectNone()
		{
			if (EventSystem.current == null || EventSystem.current.alreadySelecting) return;
			EventSystem.current.SetSelectedGameObject(null);
		}

		public static void Select(Transform tfm, bool scrollto = false)
		{
			if (EventSystem.current == null || EventSystem.current.alreadySelecting) return;
			if (tfm != null)
			{
				EventSystem.current.SetSelectedGameObject(tfm.gameObject);
				if (scrollto) ScrollTo(tfm);
			}
			else
			{
				EventSystem.current.SetSelectedGameObject(null);
			}
		}

		public static void Select(GameObject go, bool scrollto = false)
		{
			if (EventSystem.current == null || EventSystem.current.alreadySelecting) return;
			if (go != null)
			{
				EventSystem.current.SetSelectedGameObject(go);
				if (scrollto) ScrollTo(go);
			}
			else
			{
				EventSystem.current.SetSelectedGameObject(null);
			}
		}

		public static void Select([NotNull] Selectable selectable, bool scrollto = false)
		{
			if (EventSystem.current == null || EventSystem.current.alreadySelecting) return;
			if (selectable != null)
			{
				EventSystem.current.SetSelectedGameObject(selectable.gameObject);
				if (scrollto) ScrollTo(selectable);
			}
			else
			{
				EventSystem.current.SetSelectedGameObject(null);
			}
		}

		public static void ScrollTo([NotNull] Transform tfm)
		{
			if (tfm.TryGetComponent(out RectTransform rect))
			{
				ScrollRect scrollRect = rect.GetComponentInParent<ScrollRect>();
				ScrollTo(scrollRect, rect);
			}
		}

		public static void ScrollTo([NotNull] GameObject go)
		{
			if (go.TryGetComponent(out RectTransform rect))
			{
				ScrollRect scrollRect = rect.GetComponentInParent<ScrollRect>();
				ScrollTo(scrollRect, rect);
			}
		}

		public static void ScrollTo([NotNull] RectTransform rect)
		{
			ScrollRect scrollRect = rect.GetComponentInParent<ScrollRect>();
			ScrollTo(scrollRect, rect);
		}

		/*public static void ScrollTo(ScrollRect scroll, RectTransform rect)
		{
			ScrollTo(scroll, rect);
		}*/

		public static void ScrollTo([NotNull] Selectable sel)
		{
			if (sel.TryGetComponent(out RectTransform rect))
			{
				ScrollRect scrollRect = rect.GetComponentInParent<ScrollRect>();
				ScrollTo(scrollRect, rect);
			}
		}


		/// <summary>
		/// Transform the bounds of the current rect transform to the space of another transform.
		/// </summary>
		/// <param name="source">The rect to transform</param>
		/// <param name="target">The target space to transform to</param>
		/// <returns>The transformed bounds</returns>
		public static Bounds TransformBoundsTo(this RectTransform source, Transform target)
		{
			// Based on code in ScrollRect's internal GetBounds and InternalGetBounds methods
			var bounds = new Bounds();
			if (source != null)
			{
				source.GetWorldCorners(_tmpCorners);

				var vMin = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
				var vMax = new Vector3(float.MinValue, float.MinValue, float.MinValue);

				Matrix4x4 matrix = target.worldToLocalMatrix;
				for (var j = 0; j < 4; j++)
				{
					Vector3 v = matrix.MultiplyPoint3x4(_tmpCorners[j]);
					vMin = Vector3.Min(v, vMin);
					vMax = Vector3.Max(v, vMax);
				}

				bounds = new Bounds(vMin, Vector3.zero);
				bounds.Encapsulate(vMax);
			}

			return bounds;
		}

		/// <summary>
		/// Normalize a distance to be used in verticalNormalizedPosition or horizontalNormalizedPosition.
		/// </summary>
		/// <param name="axis">Scroll axis, 0 = horizontal, 1 = vertical</param>
		/// <param name="distance">The distance in the scroll rect's view's coordiante space</param>
		/// <returns>The normalized scoll distance</returns>
		public static float NormalizeScrollDistance(this ScrollRect scrollRect, int axis, float distance)
		{
			// Based on code in ScrollRect's internal SetNormalizedPosition method
			RectTransform viewport   = scrollRect.viewport;
			RectTransform viewRect   = viewport != null ? viewport : scrollRect.GetComponent<RectTransform>();
			var           viewBounds = new Bounds(viewRect.rect.center, viewRect.rect.size);

			RectTransform content       = scrollRect.content;
			Bounds        contentBounds = content != null ? content.TransformBoundsTo(viewRect) : new Bounds();

			float hiddenLength = contentBounds.size[axis] - viewBounds.size[axis];
			return distance / hiddenLength;
		}

		/// <summary>
		/// Scroll the target element to the vertical center of the scroll rect's viewport.
		/// Assumes the target element is part of the scroll rect's contents.
		/// </summary>
		/// <param name="scrollRect">Scroll rect to scroll</param>
		/// <param name="target">Element of the scroll rect's content to center vertically</param>
		public static void ScrollTo(this ScrollRect scrollRect, RectTransform target)
		{
			// The scroll rect's view's space is used to calculate scroll position
			RectTransform view = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();

			// Calcualte the scroll offset in the view's space
			Rect   viewRect      = view.rect;
			Bounds elementBounds = target.TransformBoundsTo(view);
			float  offset        = viewRect.center.y - elementBounds.center.y;

			// Normalize and apply the calculated offset
			float scrollPos = scrollRect.verticalNormalizedPosition - scrollRect.NormalizeScrollDistance(1, offset);
			scrollRect.verticalNormalizedPosition = Mathf.Clamp(scrollPos, 0f, 1f);
		}

		/// <summary>
		/// Scroll the target element to the vertical center of the scroll rect's viewport with a lerp implemented.
		/// Assumes the target element is part of the scroll rect's contents.
		/// </summary>
		/// <param name="scrollRect">Scroll rect to scroll</param>
		/// <param name="target">Element of the scroll rect's content to center vertically</param>
		public static void ScrollToWithLerp(this ScrollRect scrollRect, RectTransform target)
		{
			// The scroll rect's view's space is used to calculate scroll position
			RectTransform view = scrollRect.viewport != null ? scrollRect.viewport : scrollRect.GetComponent<RectTransform>();

			// Calcualte the scroll offset in the view's space
			Rect viewRect = view.rect;
			Bounds elementBounds = target.TransformBoundsTo(view);
			float offset = viewRect.center.y - elementBounds.center.y;

			// Normalize and apply the calculated offset
			float scrollPos = scrollRect.verticalNormalizedPosition - scrollRect.NormalizeScrollDistance(1, offset);

			ScrollRectLerper lerper = scrollRect.GetComponent<ScrollRectLerper>();

			if (lerper != null)
			{
				lerper.SetScrollDestination(scrollPos);
			}
		}

		/// <summary>
		/// Takes the existing automatic navigation and turn them
		/// into an explicit navigation that can be manipulated.
		/// Does not immediately set it on the selectable.
		/// </summary>
		/// <param name="selectable"></param>
		/// <returns></returns>
		public static Navigation ToExplicit(this Selectable selectable)
		{
			Navigation nav = selectable.navigation;


			// FindSelectableOn* functions have a misleading name, they do not find they actually are the
			// full behavior that takes the navigation mode into account. So if you set it to explicit,
			// e.g. FindSelectOnLeft will return selectOnLeft
			nav.mode              = Navigation.Mode.Automatic;
			selectable.navigation = nav;

			nav.selectOnLeft  = selectable.FindSelectableOnLeft();
			nav.selectOnRight = selectable.FindSelectableOnRight();
			nav.selectOnDown  = selectable.FindSelectableOnDown();
			nav.selectOnUp    = selectable.FindSelectableOnUp();

			nav.mode = Navigation.Mode.Explicit;
			return nav;
		}

		private static List<Selectable> _scratchSelectables = new List<Selectable>();

		public static void SetupListNavigation<T>(List<T> selectables, AxisDirection direction, bool wrap = true) where T : Selectable
		{
			if (selectables == null || selectables.Count == 0 || selectables[0] == null) return;

			Navigation.Mode _mode(AxisDirection dir) => dir == AxisDirection.Horizontal ? Navigation.Mode.Horizontal : Navigation.Mode.Vertical;

			Navigation _nav(AxisDirection dir, Selectable prev, Selectable next)
			{
				return new Navigation {
					mode          = Navigation.Mode.Explicit,
					selectOnUp    = ( dir == AxisDirection.Vertical ) 	? prev : null,
					selectOnDown  = ( dir == AxisDirection.Vertical ) 	? next : null,
					selectOnLeft  = ( dir == AxisDirection.Horizontal ) ? prev : null,
					selectOnRight = ( dir == AxisDirection.Horizontal ) ? next : null,
				};
			}

			if (selectables.Count == 1) {
				selectables[0].navigation = _nav(direction, selectables[0], selectables[0]);
				return;
			}

			_scratchSelectables.Clear();
			for (int i = 0; i < selectables.Count; i++) {
				var sel = selectables[i];
				if (sel == null) continue;

				if (!sel.interactable) {
					sel.navigation = new Navigation{mode = Navigation.Mode.None};
				} else {
					_scratchSelectables.Add(sel);
				}
			}

			for (int i = 0; i < selectables.Count; i++) {
				selectables[i].navigation = _nav(direction, selectables.WrapGet(i - 1), selectables.WrapGet(i + 1));
			}
		}

		public static void SetupGridNavigation<T>(List<T> selectables, int columns, bool hwrap = true, bool vwrap = true)
			where T: Selectable
		{
			int count = selectables.Count;

			for (var i = 0; i < count; i++)
			{
				var nav = new Navigation
				{
					mode = Navigation.Mode.Explicit
				};

				int row     = Mathf.FloorToInt(i / (float) columns);
				int column  = i % columns;
				int numRows = Mathf.CeilToInt(count / (float) columns);

				// Left and right. Wrap around on same line.

				if(hwrap) {
					nav.selectOnLeft  = column == 0 ? selectables[Mathf.Min(i + (columns - 1), selectables.Count - 1)] : selectables[i - 1];

					nav.selectOnRight = column == (columns - 1) || i >= count - 1 ? selectables[Mathf.Max(i - (columns - 1), 0)] : selectables[i + 1];
				} else {
					nav.selectOnLeft  = null;
					nav.selectOnRight = null;
				}

				// First row
				if (row == 0)
				{
					// Should loop around to the last or next to last row
					int lastRowWidth = count % columns;
					nav.selectOnUp = selectables[count - lastRowWidth + column + (column < lastRowWidth ? 0 : -columns)];
				}
				else
				{
					nav.selectOnUp = selectables[(row - 1) * columns + column];
				}

				// Last row
				if (row == numRows - 1)
				{
					nav.selectOnDown = count > columns ? selectables[column] : null;
				}
				else
				{
					int next = (row + 1) * columns + column;
					nav.selectOnDown = next >= count ? selectables[column] : selectables[(row + 1) * columns + column];
				}

				//selectables[i].onSelected = SelectCard;
				selectables[i].navigation = nav;
			}
		}

		public static Vector2Int GridSize(GridLayoutGroup grid)
		{
			int        itemsCount = grid.transform.childCount;
			Vector2Int size       = Vector2Int.zero;

			if (itemsCount == 0)
				return size;

			switch (grid.constraint)
			{
				case GridLayoutGroup.Constraint.FixedColumnCount:
					size.x = grid.constraintCount;
					size.y = getAnotherAxisCount(itemsCount, size.x);
					break;

				case GridLayoutGroup.Constraint.FixedRowCount:
					size.y = grid.constraintCount;
					size.x = getAnotherAxisCount(itemsCount, size.y);
					break;

				case GridLayoutGroup.Constraint.Flexible:
					size = flexibleSize();
					break;

				default:
					throw new ArgumentOutOfRangeException($"Unexpected constraint: {grid.constraint}");
			}

			return size;

			Vector2Int flexibleSize()
			{
				float prevX       = float.NegativeInfinity;
				int   xCount      = 0;

				for (int i = 0; i < itemsCount; i++)
				{
					Vector2 pos = ((RectTransform)grid.transform.GetChild(i)).anchoredPosition;

					if (pos.x <= prevX)
						break;

					prevX = pos.x;
					xCount++;
				}

				int yCount = getAnotherAxisCount(itemsCount, xCount);
				return new Vector2Int(xCount, yCount);
			}

			int getAnotherAxisCount(int totalCount, int axisCount) => totalCount / axisCount + Mathf.Min(1, totalCount % axisCount);
		}

		public static void SelectNext(Transform root, bool scrollto = false)
		{
			if (SelectedObject == null) return;

			Transform tfm = SelectedObject.transform;
			if (tfm.parent != root) return;

			int idx = tfm.GetSiblingIndex();
			if (idx != root.childCount - 1)
			{
				Select(root.GetChild(idx + 1));
			}
		}

		public static void SelectPrevious(Transform root, bool scrollto = false)
		{
			if (SelectedObject == null) return;

			Transform tfm = SelectedObject.transform;
			if (tfm.parent != root) return;

			int idx = tfm.GetSiblingIndex();
			if (idx > 0)
			{
				Select(root.GetChild(idx - 1));
			}
		}

		public static bool SelectNearest(Transform root)
		{
			if (SelectedObject == null) return false;
			return SelectNearest(root, SelectedObject.transform.position);
		}

		public static bool SelectNearest([NotNull] Transform root, Vector3 origin)
		{
			if (root == null) return false;
			if (root.childCount == 0) return false;
			if (root.childCount == 1)
			{
				Select(root.GetChild(0));
				return true;
			}

			Transform nearest  = null;
			float     distance = float.MaxValue;

			for (var i = 0; i < root.childCount; i++)
			{
				Transform child = root.GetChild(i);

				float d = Vector3.Distance(origin, child.position);
				if (d < distance)
				{
					nearest  = child;
					distance = d;
				}
			}

			Select(nearest);
			return true;
		}

		public static void SelectTowards(Transform root, Vector3 origin, Vector3 direction)
		{
			throw new NotImplementedException();
		}

		public static bool SelectTowards(Vector3 origin, Vector3 direction)
		{
			Transform nearest  = null;
			float     distance = float.MaxValue;

			Selectable[] selectables = ArrayPool<Selectable>.Claim(Selectable.allSelectableCount);
			var          ray         = new Ray(origin, direction);

			Selectable.AllSelectablesNoAlloc(selectables);

			for (var i = 0; i < Selectable.allSelectableCount; i++)
			{
				Selectable child = selectables[i];

				if (!child.IsInteractable())
					continue;

				Transform tfm = child.transform;
				float     d   = MathUtil.DistanceToRay(tfm.position, ray);

				if (d < distance)
				{
					nearest  = tfm;
					distance = d;
				}
			}

			ArrayPool<Selectable>.Release(ref selectables);
			Select(nearest);

			return true;
		}
	}
}