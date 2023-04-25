using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


[RequireComponent(typeof(ScrollRect))]
public class ScrollViewAutoScrollerGrid : MonoBehaviour, IScrollHandler
{
	public int   columnCount;
	public float scrollLerp;
	public float mouseWheelScrollSpeed;

	public float           _verticalPosition;
	public ScrollRect      _scrollRect;
	public Scrollbar       _scrollBar;
	public GridLayoutGroup _gridLayout;

	private SelectableMouseOverDetector _scrollBarDetector;

	private RectTransform _rectTransform;
	//private List<Selectable>   _selectables;

	public void Start()
	{
		_rectTransform     = GetComponent<RectTransform>();
		_scrollRect        = GetComponent<ScrollRect>();
		_scrollBarDetector = _scrollBar.transform.GetComponent<SelectableMouseOverDetector>();

		//_selectables    = GetComponentsInChildren<Selectable>();

		//m_buttons[m_index].Select();
		//verticalPosition = 1f - ((float)index / (_selectables.Length - 1));
	}

	private void Update()
	{
		var contentSize  = _scrollRect.content.rect.size;
		var viewPortSize = _scrollRect.viewport.rect.size;

		_scrollBar.size                 = contentSize.y <= 0.0 ? 1f : Mathf.Clamp01((viewPortSize.y - Mathf.Abs(0)) / contentSize.y);
		_scrollBar.handleRect.offsetMax = Vector2.zero;
		_scrollBar.handleRect.offsetMin = Vector2.zero;

		_scrollRect.verticalNormalizedPosition = Mathf.Lerp(_scrollRect.verticalNormalizedPosition, _verticalPosition, scrollLerp);
		_scrollBar.value                       = _verticalPosition;
	}

	public void UpdateTargetElement(int selectedIndex, int numElements)
	{
		int row  = selectedIndex / columnCount;
		int rows = Mathf.CeilToInt(numElements / columnCount);

		float targetPos = _verticalPosition;

		var contentSize            = _scrollRect.content.rect.size;
		var viewPortSize           = _scrollRect.viewport.rect.size;
		var normalizedSize         = (viewPortSize.y / contentSize.y);
		var normalizedGridCellSize = (_gridLayout.cellSize.y + _gridLayout.spacing.y) / contentSize.y;

		targetPos = 1f - ((float) row / rows - normalizedSize / 2) * (1.0f + normalizedSize) - normalizedGridCellSize / 2 /*- (gridLayout.spacing.y / contentSize.y)*/;


		_verticalPosition = Mathf.Clamp01(targetPos);

		/*m_up   = Input.GetKeyDown(KeyCode.UpArrow);
		m_down = Input.GetKeyDown(KeyCode.DownArrow);*/

		/*if (m_up ^ m_down)
		{
			if (m_up)
				index = Mathf.Clamp(index - 1, 0, _selectables.Length - 1);
			else
				index = Mathf.Clamp(index + 1, 0, _selectables.Length - 1);

			_selectables[index].Select();
			verticalPosition = 1f - ((float)index / (_selectables.Length - 1));
		}*/
	}

	//Tie this to the unityevent on the scroll view to make sure the scroll bar still works.
	public void OnValueChanged(Vector2 pos)
	{
		//Debug.Log("ScrollRect Value Changed: "+pos);
		/*verticalPosition = pos.y;
		scrollRect.verticalNormalizedPosition = pos.y;*/
	}

	//For some reason the parameter was always 0 here.
	public void OnScrollbarValueChanged(float pos)
	{
		//Vector2 localMousePos = scrollBar.handleRect.InverseTransformPoint(Input.mousePosition);

		//Debug.Log(localMousePos);

		//Not the most air tight. We just need a way to know if the user is actually dragging on the scroll wheel.
		if (_scrollBarDetector.IsOver)
		{
			//Debug.Log("Scrollbar Value Changed: " + scrollBar.value);
			_verticalPosition                      = _scrollBar.value;
			_scrollRect.verticalNormalizedPosition = _scrollBar.value;
		}
	}

	public void OnScroll(PointerEventData eventData)
	{
		// Vector2 ScrollDelta = eventData.scrollDelta;
		//
		// verticalPosition = Mathf.Clamp01(verticalPosition + ScrollDelta.y * mouseWheelScrollSpeed);
		//
		// ContentRef.anchoredPosition += new Vector2(0, -ScrollDelta.y * ScrollSpeed);
		//
		// if (ContentRef.anchoredPosition.y < MinScroll)
		// {
		// 	ContentRef.anchoredPosition = new Vector2(0, MinScroll);
		// }
		// else if (ContentRef.anchoredPosition.y > MaxScroll)
		// {
		// 	ContentRef.anchoredPosition = new Vector2(0, MaxScroll);
		// }
	}
}