using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class SettingsPageTab : MonoBehaviour
{
	[SerializeField] private float unselectedHeight;
	[SerializeField] private float selectedHeight;

	[Space]

	[SerializeField] private Color selectedBackgroundColor;
	[SerializeField] private Color unselectedBackgroundColor;

	[Space]

	[SerializeField] private Color selectedTextColor;
	[SerializeField] private Color unselectedTextColor;
	[SerializeField] private Color hoveredTextColor;

	[Space]

	[SerializeField] private Image background;

	[Space]

	[SerializeField] private Sprite selectedBackground;
	[SerializeField] private Sprite unselectedBackground;

	[Space]

	[SerializeField] private TMP_Text label;

	[System.NonSerialized] public bool Selected;
	[System.NonSerialized] public bool Hovered;

	private RectTransform rectTransform;

	public void ToggleSelection(bool selected)
	{
		Selected = selected;

		Vector2 position = rectTransform.anchoredPosition;
		position.y = (Selected ? selectedHeight : unselectedHeight);
		rectTransform.anchoredPosition = position;

		background.sprite = (Selected ? selectedBackground : unselectedBackground);
		background.color = (Selected ? selectedBackgroundColor : unselectedBackgroundColor);
	}

	public void OnPointerEnter(BaseEventData eventData)
	{
		Hovered = true;
	}

	public void OnPointerExit(BaseEventData eventData)
	{
		Hovered = false;
	}

	// Start is called before the first frame update
	public void Initialize()
    {
		Selected = false;
		Hovered = false;

		if (rectTransform == null)
		{
			rectTransform = GetComponent<RectTransform>();
		}
    }

    // Update is called once per frame
    void Update()
    {
		if (Selected)
			label.color = selectedTextColor;
		else if (Hovered)
			label.color = hoveredTextColor;
		else
			label.color = unselectedTextColor;
	}
}
