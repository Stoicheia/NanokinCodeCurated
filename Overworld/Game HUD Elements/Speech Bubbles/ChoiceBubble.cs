using System;
using System.Collections.Generic;
using Anjin.Nanokin;
using Anjin.Util;
using TMPro;
using UnityEngine;

namespace Anjin.UI
{
	public class ChoiceBubble : HUDBubble
	{
		public Color DeselectedTint;
		public Color SelectedTint;

		public List<GameText> 	Choices;
		public int 				ChoiceSelected;

		public TextMeshProUGUI ChoiceTemplate;
		public List<TextMeshProUGUI> InstantiatedChoices;

		public RectTransform ChoicesContainer;

		public Action<int> OnSelected;

		public GameInputs.Button Up;
		public GameInputs.Button Down;
		public GameInputs.ActionButton Confirm;

		public override void Awake()
		{
			base.Awake();
			InstantiatedChoices = new List<TextMeshProUGUI>();

			Up 		= GameInputs.move.up;
			Down 	= GameInputs.move.down;
			Confirm = GameInputs.confirm;
		}

		public override void Update()
		{
			base.Update();

			if (state == State.On) {
				if (Up.IsPressed) {
					ChoiceSelected--;
					if (ChoiceSelected < 0)
						ChoiceSelected = Choices.Count - 1;
				} else if (Down.IsPressed) {
					ChoiceSelected++;
					if (ChoiceSelected > Choices.Count - 1)
						ChoiceSelected = 0;
				}

				if (Confirm.IsPressed) {
					OnSelected?.Invoke(ChoiceSelected);
					StartDeactivation(true);
				}

				for (int i = 0; i < InstantiatedChoices.Count; i++) {
					InstantiatedChoices[i].text = ( i == ChoiceSelected ) ? ">" + Choices[i] : (string)Choices[i];
					InstantiatedChoices[i].color = ( i == ChoiceSelected ) ? SelectedTint : DeselectedTint;
				}

			}
		}


		public override void OnDone()
		{
			for (int i = 0; i < InstantiatedChoices.Count; i++)
				InstantiatedChoices[i].gameObject.Destroy();

			InstantiatedChoices.Clear();
			Choices.Clear();

			DeactivateFinish();
		}

		void InstantiateChoices()
		{
			for (int i = 0; i < Choices.Count; i++) {
				var obj = Instantiate(ChoiceTemplate, ChoicesContainer);

				obj.gameObject.SetActive(true);
				obj.text = Choices[i];

				InstantiatedChoices.Add(obj);
			}

		}

		public void Show(List<GameText> choices, int selected, Action<int> onSelected)
		{
			if (state != State.Off) return;

			Choices 		= choices;
			ChoiceSelected 	= selected;
			OnSelected 		= onSelected;

			InstantiateChoices();
			StartActivation();
			ChoiceTemplate.gameObject.SetActive(false);
		}
	}
}