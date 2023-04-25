using System;
using Anjin.Nanokin;
using Anjin.Scripting;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Overworld.UI {
	[LuaUserdata]
	public class SimpleSplashScreen : SerializedMonoBehaviour, ISplashScreen {

		public LuaScriptContainer Script = new LuaScriptContainer(true);

		public LabelWithInputButton ContinueLabel;
		public float                ContinueLabelDelay = 1f;

		[NonSerialized, ShowInPlay] public bool  ContinueLabelActive  = true;
		[NonSerialized, ShowInPlay] public float ContinueLabelTimer;

		private void Start()
		{
			Script.OnStart(this, container => {
				container._state.table["splash_screen"] = this;
			});

			if(ContinueLabel) {
				ContinueLabel.Label.text         = "Continue";
				ContinueLabel.InputButton.Button = GameInputs.confirm;

				if (ContinueLabelDelay > 0) {
					ContinueLabelTimer  = 1;
					ContinueLabelActive = false;
					ContinueLabel.gameObject.SetActive(false);
				}
			}
		}

		private void Update()
		{
			if (SplashScreens.IsShowing) {

				if(!ContinueLabelActive) {
					if (ContinueLabelTimer > 0)
						ContinueLabelTimer -= Time.deltaTime;
					else {
						ContinueLabelActive = true;
						ContinueLabel.gameObject.SetActive(true);
					}
				} else {
					if (GameInputs.confirm.IsPressed) {
						Hide();
					}
				}
			}
		}

		private async void Hide()
		{
			if (Script.Script != null) {
				await Script.TryPlayAsync("on_hide");
			}
			await SplashScreens.Hide();
		}

		public async UniTask OnShow()
		{
			if (Script.Script != null) {
				await Script.TryPlayAsync("on_show");
			}
			await SplashScreens.FadeBackdrop(Color.black, 0, 1, 1);
			//return UniTask.CompletedTask;
			//return ;
		}

		public async UniTask OnHide()
		{
			await SplashScreens.FadeBackdrop(Color.black, 1, 0, 1);
		}

		public Color?  GetBackdropColor() => Color.black;
	}
}