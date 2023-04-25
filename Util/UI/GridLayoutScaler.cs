using Anjin.EditorUtility;
using UnityEngine;
using UnityEngine.UI;
using Util;

public class GridLayoutScaler : MonoBehaviour
{
	GridLayoutGroup gridLayoutGroup;
	RectTransform   rect;
	public float    height;
	public int      cellCount = 2;

	public Vector2 normalSize;
	public float   maxScale = 1.5f;

	public Axis2D constraintAxis = Axis2D.x;

	void Awake()
	{
		gridLayoutGroup = GetComponent<GridLayoutGroup>();
		rect            = GetComponent<RectTransform>();
		normalSize      = gridLayoutGroup.cellSize;
	}

	// TODO(C.L.): MAYBE figure out how to do this automatically on BOTH axes.
	public void Fix()
	{
		if (gridLayoutGroup == null) return;

		//float smaller_axis = rect.rect.size.x > rect.rect.size.y ? rect.rect.size.y : rect.rect.size.x;

		Vector2Int grid_size = UGUI.GridSize(gridLayoutGroup);

		/*Vector2 hscale = rect.rect.size / (normalSize * grid_size.x);
		Vector2 vscale = rect.rect.size / (normalSize * grid_size.y);

		float haspect = (float)grid_size.x / (float)grid_size.y;
		float vaspect = (float)grid_size.y / (float)grid_size.x;*/

		Vector2 scale = rect.rect.size / (normalSize * grid_size);

		Vector2 size = normalSize * (constraintAxis == Axis2D.x ? scale.x : scale.y);

		gridLayoutGroup.cellSize = new Vector2(
			Mathf.Min(size.x, normalSize.x * maxScale),
			Mathf.Min(size.y, normalSize.y * maxScale)
		);
		return;

		if (grid_size.x > grid_size.y) {
			// If the grid is wider than it is tall


			//gridLayoutGroup.cellSize = normalSize * ;
		}

		Vector2 sizePercentage;
		sizePercentage = rect.rect.size / (normalSize * grid_size);

		/*if(rect.rect.size.x > rect.rect.size.y)
			sizePercentage = rect.rect.size / (normalSize * grid_size.x);
		else
			sizePercentage = rect.rect.size / (normalSize * grid_size.y);*/

		//float smaller_ = (sizePercentage.x > sizePercentage.y ? sizePercentage.y : sizePercentage.x);
		float smaller = (rect.rect.size.x > rect.rect.size.y ? sizePercentage.x : sizePercentage.y);
		gridLayoutGroup.cellSize = normalSize * smaller;

	}

	void OnRectTransformDimensionsChange() => Fix();
}