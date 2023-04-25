using Anjin.Nanokin;
using Combat.Components;
using Combat.UI.TurnOrder;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Util.UniTween.Value;

namespace Combat.UI
{
	public class CombatUI : StaticBoy<CombatUI>
	{
		[SerializeField, SceneObjectsOnly] public GameObject               Root_OverdriveUI;
		[SerializeField, SceneObjectsOnly] public Transform                Root_OverdriveEntries;
		[SerializeField, SceneObjectsOnly] public OverdriveButtonHintPanel OverdriveButtonHint;

		[Title("Animation")]
		[SerializeField] public PostProcessVolume OverdrivePostfx;
		[SerializeField] public EaserTo OverdrivePostfxIn;
		[SerializeField] public EaserTo OverdrivePostfxOut;

		public InputButtonLabel OverdriveUpLabel;
		public InputButtonLabel OverdriveDownLabel;
		public InputButtonLabel ShowInfoLabel;

		private TweenableFloat _overdriveProgress;

		public void SetVisible(bool b, bool turnUI = true)
		{
			OverdriveButtonHint.gameObject.SetActive(false);

			StatusUI.SetVisible(b);
			ComboUI.SetVisible(b);
			TurnUI.SetVisible(b && turnUI);
		}

		protected override void OnAwake()
		{
			base.OnAwake();

			_overdriveProgress = new TweenableFloat();
		}

		private void Start()
		{
			SetVisible(false);

			OverdriveUpLabel.Button = GameInputs.overdriveUp;
			OverdriveDownLabel.Button = GameInputs.overdriveDown;
			ShowInfoLabel.Button = GameInputs.showInfo;
		}

		public static void SetOverdriveInput(bool active)
		{
			Live._overdriveProgress.To(active ? 0.25f : 0, EaserTo.Linear);
		}

		public static void SetOverdriveActive(bool enable)
		{
			if (enable)
			{
				Live._overdriveProgress.To(1, Live.OverdrivePostfxIn);
			}
			else
			{
				Live._overdriveProgress.To(0, Live.OverdrivePostfxOut);
			}
		}

		private void Update()
		{
			OverdrivePostfx.weight = _overdriveProgress.value;
		}
	}
}