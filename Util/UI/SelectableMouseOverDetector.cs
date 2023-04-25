using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Selectable))]
public class SelectableMouseOverDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
	[ShowInInspector]
	public bool IsOver { get; private set; }

	private bool _isOver;

	private void Update()
	{
		if (!Input.GetMouseButton(0) && !_isOver) IsOver = false;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		IsOver = true;
		_isOver = true;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		_isOver = false;
	}
}
