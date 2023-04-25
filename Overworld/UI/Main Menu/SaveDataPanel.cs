using System;
using System.Collections.Generic;
using SaveFiles;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SaveDataPanel : SerializedMonoBehaviour/*, IPointerClickHandler, ISubmitHandler, ISelectHandler*/
{
	[SerializeField] private float moveDuration = 0.2f;

	[SerializeField] private GameObject		 Empty_Save_Label;

	[SerializeField] private TextMeshProUGUI TMP_Name;
	[SerializeField] private TextMeshProUGUI TMP_Progression;
	[SerializeField] private TextMeshProUGUI TMP_Playtime;
	[SerializeField] private TextMeshProUGUI TMP_PartyMembers;
	[SerializeField] private TextMeshProUGUI TMP_Money;
	//[SerializeField] private Button          BTN_Delete;
	[SerializeField] private Image			 Backing;
	[SerializeField] private Image			 FreeSlotBorder;

	[SerializeField] private Color			 SelectedColor;
	[SerializeField] private Color			 UnselectedColor;
	[SerializeField] private Color			 EnabledColor;
	[SerializeField] private Color			 DisabledColor;


	[NonSerialized] public int                   index;
	[NonSerialized] public SaveFileID?           id;

	[NonSerialized] public SaveData              save;
	[NonSerialized] public Action<SaveDataPanel> onSelected;
	[NonSerialized] public Action<SaveDataPanel> onDeselected;
	[NonSerialized] public Action<SaveDataPanel> onConfirmed;
	[NonSerialized] public Action<SaveDataPanel> onDelete;

	[SerializeField] private List<GameObject> Busts;
	[SerializeField] private List<TextMeshProUGUI> LevelDisplays;

	private bool movingToPosition;

	private float distanceToTarget;
	private float moveSpeed;

	private Vector2 targetPosition;

	private void Awake()
	{
		movingToPosition = false;
	}

	public void SetSave(SaveData save, int index)
	{
		this.save  = save;
		this.index = index;

		if(save != null) {
			id = save.ID.Value;
		} else {
			id = null;
		}

		if(id.HasValue) {
			int displayIndex = id.Value.index + 1;

			if (displayIndex >= 10)
				TMP_Name.text = $"{displayIndex}";
			else
				TMP_Name.text = $"0{displayIndex}";
		}

		if (save != null)
		{
			FreeSlotBorder.color = EnabledColor;
			Empty_Save_Label.SetActive(false);

			TMP_Progression.text = "NEW FILE";
			//TMP_PartyMembers.text = $"{save.Party.Count} party member{(save.Party.Count >= 2 ? "s" : "")}";
			TMP_Playtime.text = $"Playtime: {TimeSpan.FromSeconds(save.LastTimeSaved).ToString("hh\\:mm\\:ss")}";
			TMP_Money.text = $"Credits: {save.Money}";

			int partyCount = save.Party.Count;

			for (int i = 0; ((i < Busts.Count) && (i < partyCount)); i++)
			{
				LevelDisplays[i].text = $"Lv {save.Party[i].Level}";
				LevelDisplays[i].gameObject.SetActive(true);

				Busts[i].SetActive(true);
			}
		}
		else
		{
			TMP_Progression.text = "";
			//TMP_PartyMembers.text = $"{save.Party.Count} party member{(save.Party.Count >= 2 ? "s" : "")}";
			TMP_Playtime.text = "";
			TMP_Money.text = "";

			for (int i = 0; i < Busts.Count; i++)
			{
				Busts[i].SetActive(false);
			}

			for (int i = 0; i < LevelDisplays.Count; i++)
			{
				TextMeshProUGUI levelDisplay = LevelDisplays[i];
				levelDisplay.gameObject.SetActive(false);
				levelDisplay.text = "";
			}

			FreeSlotBorder.color = DisabledColor;
			Empty_Save_Label.SetActive(true);
		}
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
		onSelected?.Invoke(this);
	}

	public void OnDeselect(BaseEventData eventData)
	{
		Backing.color = UnselectedColor;
		//onDeselected?.Invoke(this);
	}

	public void ToggleColor(bool selected)
	{
		Backing.color = (selected ? SelectedColor : UnselectedColor);
	}

	public void TogglePosition(bool selected)
	{
		targetPosition = Backing.rectTransform.anchoredPosition;
		targetPosition.x = (selected ? 108 : 0);

		distanceToTarget = Vector2.Distance(Backing.rectTransform.anchoredPosition, targetPosition);

		moveSpeed = distanceToTarget / moveDuration;

		movingToPosition = true;
	}

	public void ImmediatelySelect()
	{
		Backing.color = SelectedColor;
		Backing.rectTransform.anchoredPosition = new Vector2(108, Backing.rectTransform.anchoredPosition.y);
	}

	private void Update()
	{
		if (movingToPosition)
		{
			Backing.rectTransform.anchoredPosition = Vector2.MoveTowards(Backing.rectTransform.anchoredPosition, targetPosition, (moveSpeed * Time.deltaTime));

			if (Vector2.Distance(Backing.rectTransform.anchoredPosition, targetPosition) <= 0.05f)
			{
				movingToPosition = false;
			}
		}
	}
}