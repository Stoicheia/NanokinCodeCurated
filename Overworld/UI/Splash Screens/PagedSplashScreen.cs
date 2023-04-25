using System;
using System.Collections.Generic;
using Anjin.Nanokin;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Util.Odin.Attributes;

namespace Overworld.UI {
	public class PagedSplashScreen : MonoBehaviour, ISplashScreen {

		public Transform PageRoot;

		public TMP_Text             PageLabel;

		public Image                LeftIndicator;
		public Image                RightIndicator;
		public LabelWithInputButton ContinueLabel;

		[NonSerialized,ShowInPlay]
		public List<Transform> Pages;

		[NonSerialized, ShowInPlay]
		private int _currentPage;

		[SerializeField] private AudioDef _sfxScrollBack;
		[SerializeField] private AudioDef _sfxScrollNext;

		private void Awake()
		{
			Pages = new List<Transform>();
			for (int i = 0; i < PageRoot.childCount; i++) {
				Pages.Add(PageRoot.GetChild(i));
			}

			if (ContinueLabel)
				ContinueLabel.InputButton.Button = GameInputs.confirm;

			Button leftButton = LeftIndicator.AddComponent<Button>();
			Button rightButton = RightIndicator.AddComponent<Button>();
			Button exitButton = ContinueLabel.AddComponent<Button>();

			leftButton.onClick.AddListener(MoveLeft);
			rightButton.onClick.AddListener(MoveRight);
			exitButton.onClick.AddListener(Exit);
		}

		public void MoveLeft()
		{
			_currentPage--;

			if (_currentPage >= 0)
			{
				GameSFX.PlayGlobal(_sfxScrollBack, transform, .97f, -1);
			}
		}

		public void MoveRight()
		{
			bool on_last = _currentPage == Pages.Count - 1;
			_currentPage++;

			if (!on_last)
			{
				GameSFX.PlayGlobal(_sfxScrollNext, transform, 1, -1);
			}
		}

		public void Exit()
		{
			SplashScreens.Hide();
		}

		private void Update()
		{
			bool on_last = _currentPage == Pages.Count - 1;
			if (SplashScreens.IsShowing) {

				if (GameInputs.move.left.IsPressed || GameInputs.menuNavigate.left.IsPressed)
				{
					MoveLeft();
				}

				if (GameInputs.move.right.IsPressed || GameInputs.menuNavigate.right.IsPressed)
				{
					MoveRight();
				}

				if (/*on_last && */GameInputs.confirm.IsPressed) {
					Exit();
				}
			}

			_currentPage = Mathf.Clamp(_currentPage, 0, Pages.Count - 1);

			for (var i = 0; i < Pages.Count; i++) {
				Transform page = Pages[i];
				page.gameObject.SetActive(_currentPage == i);
			}

			LeftIndicator.gameObject.SetActive(_currentPage  > 0);
			RightIndicator.gameObject.SetActive(_currentPage < Pages.Count - 1);

			ContinueLabel.gameObject.SetActive(true);

			PageLabel.text = $"Page {_currentPage + 1}/{Pages.Count}";

		}

		public async UniTask OnShow() { }
		public async UniTask OnHide() { }
	}
}