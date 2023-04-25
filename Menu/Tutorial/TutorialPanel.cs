using Anjin.Core.Flags;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TutorialPanel : SerializedMonoBehaviour
{
	[SerializeField] private Color avaiableColor;
	[SerializeField] private Color unavaiableColor;
	[SerializeField] private Color selectedColor;
	[SerializeField] private Color unselectedColor;

	//[SerializeField] private GameObject tutorial;

	[SerializeField] private Image background;

	[SerializeField] private TextMeshProUGUI titleLabel;

	[System.NonSerialized] public System.Action<TutorialPanel> onSelected;
	[System.NonSerialized] public System.Action<TutorialPanel> onDeselected;
	[System.NonSerialized] public System.Action<TutorialPanel> onConfirmed;

	private string flag;
	private string title;
	private string address;

	public bool Viewed { get; private set; }

	public int ID { get; private set; }

	public string Address => address;

	public void ToggleColor(bool selected)
	{
		background.color = (selected ? selectedColor : (Viewed ? unselectedColor : unavaiableColor));
	}

	public void ImmediatelySelect()
	{
		background.color = selectedColor;
		//background.rectTransform.anchoredPosition = new Vector2(108, background.rectTransform.anchoredPosition.y);
	}

	public void OnPointerClick(BaseEventData eventData)
	{
		onConfirmed?.Invoke(this);
	}

	public void OnSubmit(BaseEventData eventData)
	{
		onConfirmed?.Invoke(this);
	}

	public void OnSelect(BaseEventData eventData)
	{
		background.color = selectedColor;
	}

	public void OnDeselect(BaseEventData eventData)
	{
		background.color = (Viewed ? unselectedColor : unavaiableColor);
	}

	// Start is called before the first frame update
	public void Initialize(int ID, TutorialEntryInfo info)
    {
		this.ID = ID;

		flag = info.Flag;
		title = info.Title;
		address = info.Address;

		Viewed = Flags.GetBool(flag);
		background.color = (Viewed ? avaiableColor : unavaiableColor);
		titleLabel.text = (Viewed ? title : "???");
    }
}
