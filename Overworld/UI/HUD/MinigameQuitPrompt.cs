using System;
using Anjin.Nanokin;
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.UI
{
	public class MinigameQuitPrompt : StaticBoy<MinigameQuitPrompt> {


		[SerializeField] private GameObject prompt;

		[SerializeField] private Color SelectedTextColor;
		[SerializeField] private Color UnselectedTextColor;

		[SerializeField] private TextMeshProUGUI QuitYesButtonLabel;
		[SerializeField] private TextMeshProUGUI QuitNoButtonLabel;

		[NonSerialized, ShowInPlay]
		public bool Active;

		public override void Awake()
		{
			base.Awake();
			Active = false;
			prompt.SetActive(false);
		}

		public void Show()
		{
			if (Active) return;
			Active = true;

			prompt.SetActive(true);
			GameInputs.mouseUnlocks.Add("minigame_quit_prompt");
			GameController.Live.CurrentMinigame.ToggleControlFromPrompt(false);
		}

		public void ToggleSaveYesColor(bool selected)
		{
			QuitYesButtonLabel.color = (selected ? SelectedTextColor : UnselectedTextColor);
		}

		public void ToggleSaveNoColor(bool selected)
		{
			QuitNoButtonLabel.color = (selected ? SelectedTextColor : UnselectedTextColor);
		}

		public void PerformQuitAction(bool quitting)
		{
			prompt.SetActive(false);
			Active = false;
			GameInputs.mouseUnlocks.Remove("minigame_quit_prompt");
			GameController.Live.CurrentMinigame.ToggleControlFromPrompt(true);

			if (quitting)
				GameController.Live.CurrentMinigame.Quit();
		}

		private void Update()
		{
			if (prompt.activeSelf)
			{
				if (GameInputs.confirm.IsPressed)
				{
					PerformQuitAction(true);
				}
				else if (GameInputs.cancel.IsPressed || GameInputs.splicer.IsPressed)
				{
					PerformQuitAction(false);
				}
			}
		}
	}
}
