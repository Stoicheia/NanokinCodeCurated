using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Anjin.EditorUtility;
using Anjin.Nanokin;
using Anjin.Util;
using Assets.Scripts.Utils;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;
using Data.Nanokin;
using Data.Shops;
using JetBrains.Annotations;
using SaveFiles;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using Util.Addressable;
using Util.Components.UI;
using Object = UnityEngine.Object;

namespace Combat.Components.VictoryScreen.Menu
{
	public class VictoryUI : StaticBoy<VictoryUI>
	{
		[Title("UI References")]
		[SerializeField] private TextMeshProMulti Label_TotalXP;
		[SerializeField] private TextMeshProMulti Label_TotalRP;
		[SerializeField] private List<TextMeshProMulti> Label_Loots;
		[SerializeField] private List<Image> Image_Loots;
		[SerializeField] private TextMeshProMulti Label_TotalCredits;
		[SerializeField] private TextMeshProMulti Label_PressAnyToContinue;
		[SerializeField] private RectTransform    Root_ScreenUI;
		[SerializeField] private RectTransform    Root_CharacterUI;
		[SerializeField] private CanvasGroup      Group_ScreenUI;
		[SerializeField] private CanvasGroup      Group_CharacterUI;

		[Title("Prefabs", horizontalLine: false)]
		[SerializeField] private VictoryCharacterUI CharacterUIPrefab;
		[SerializeField] private NotificationRectStack CharacterNotificationStackPrefab;
		[SerializeField] private GameObject            LevelUpPanelPrefab;
		[SerializeField] private GameObject            MasteryUpPanelPrefab;

		[Title("Income Distribution")]
		[SerializeField] public AnimationCurve IncomeDistributionDuration;
		[SerializeField] private float    IncomeSfxPitchChange;
		[SerializeField] private float    IncomeSfxPitchMax;
		[SerializeField] private AudioDef IncomeTickSFX;
		[SerializeField] public  AudioDef LevelUpSFX;
		[SerializeField] public  AudioDef MasteryUpSFX;

		[Title("FX")]
		[SerializeField] private GameObject FX_IncomeDistributionLoopingPrefab;
		[SerializeField] private GameObject FX_LevelUpPrefab;
		[SerializeField] private GameObject FX_MasteryUpPrefab;

		// Gain Distribution
		// ----------------------------------------
		[NonSerialized] public TickerValue   xpTicker;
		[NonSerialized] public TickerValue   rpTicker;
		[NonSerialized] public float         distributionDuration;
		[NonSerialized] public bool          enableDistribution;
		[NonSerialized] public List<Fighter> fighters;

		private List<LootEntry> itemLoots;

		private float _incomeTickPitch; // The pitch of the ticking sound effect whenever a XP point is gained. Rises as more XP is gained, creating a satisfying effect.

		private AsyncHandles    _handles;
		private List<CharEntry> _entries;
		// private List<VictoryCharacterUI>    _entries;
		// private List<NotificationRectStack> _entries;
		private ArenaVictoryController _ctrl;
		private ManualVFX              _fighterVFX;

		private class CharEntry
		{
			public CharacterEntry        character;
			public GameObject            actorObject;
			public VictoryCharacterUI    ui;
			public NotificationRectStack notifications;

			public ParticleRef loopingTickParticles;

			public void Destroy()
			{
				Object.Destroy(ui.gameObject);
				Object.Destroy(notifications.gameObject);
				loopingTickParticles.SetAutoDestroy();
			}

			public CharEntry(GameObject actorObject, CharacterEntry @char, VictoryCharacterUI ui, NotificationRectStack notifications)
			{
				this.actorObject   = actorObject;
				this.character     = @char;
				this.ui            = ui;
				this.notifications = notifications;
			}
		}

		public bool FinishedIncomeDistribution => _entries.Count == 0 || (xpTicker.remaining <= 0 && rpTicker.remaining <= 0);

		public override void Awake()
		{
			base.Awake();

			_handles = new AsyncHandles();
			_entries = new List<CharEntry>();
			Label_PressAnyToContinue.gameObject.SetActive(false);
		}

	#region UI

		private async UniTask RefreshTotalLabels()
		{
			const string NO_LOOT_TEXT = "No Loots";
			Label_TotalXP.Text = xpTicker.ToString();
			Label_TotalRP.Text = rpTicker.ToString();
			foreach (TextMeshProMulti t in Label_Loots)
			{
				t.gameObject.SetActive(false);
			}
			if (itemLoots.Count == 0)
			{
				Label_Loots[0].Text = NO_LOOT_TEXT;
			}
			else
			{
				for (int i = 0; i < Mathf.Min(itemLoots.Count, Label_Loots.Count); i++)
				{
					Label_Loots[i].Text = itemLoots[i].GetName();
					Label_Loots[i].gameObject.SetActive(true);
				}
			}

			foreach (Image image in Image_Loots)
			{
				image.gameObject.SetActive(false);
			}

			for (int i = 0; i < Mathf.Min(itemLoots.Count, Image_Loots.Count); i++)
			{
				var display = await itemLoots[i].LoadDisplay();

				switch (display.method)
				{
					case LootDisplayMethod.Sprite:
						Image_Loots[i].sprite = display.sprite;
						Label_Loots[i].gameObject.SetActive(true);
						break;
					default:
						break;
				}
			}

		}

	#endregion

	#region Input

		public async UniTask SetIncome(int xp, int rp)
		{
			xpTicker = new TickerValue(xp);
			rpTicker = new TickerValue(rp);

			_incomeTickPitch = 1;

			await RefreshTotalLabels();
		}

		public void AddCharacter(GameObject actor, [NotNull] CharacterEntry character)
		{
			// Character UI (xp bar, text, etc.)
			VictoryCharacterUI ui = Instantiate(CharacterUIPrefab, Root_CharacterUI.transform, false);
			ui.Show(character, actor).ForgetWithErrors();
			ui.index = _entries.Count;


			// Notification Stack UI
			NotificationRectStack stack   = Instantiate(CharacterNotificationStackPrefab, Root_CharacterUI.transform, false);
			WorldToCanvasRaycast  raycast = stack.GetOrAddComponent<WorldToCanvasRaycast>();
			raycast.SetWorldPos(actor.transform);

			var entry = new CharEntry(actor, character, ui, stack);
			entry.loopingTickParticles = ParticleRef.Instantiate(FX_IncomeDistributionLoopingPrefab, actor.transform.position, null);
			entry.loopingTickParticles.SetActive(false);

			_entries.Add(entry);
		}

		/// <summary>
		/// Remove all characters from the victory menu.
		/// </summary>
		public void ClearCharacters()
		{
			foreach (CharEntry entry in _entries)
			{
				entry.Destroy();
			}

			_entries.Clear();
		}

		public void SetItemLoots(List<LootEntry> loots)
		{
			itemLoots = loots;
		}

	#endregion

		public void LateUpdate()
		{
			if (_ctrl != null)
			{
				Group_ScreenUI.alpha              = _ctrl.ScreenUIOpacity;
				Group_CharacterUI.alpha           = _ctrl.CharacterUIOpacity;
				Root_ScreenUI.anchoredPosition    = Vector3.up * _ctrl.ScreenUIEntrance;
				Root_CharacterUI.anchoredPosition = Vector3.down * _ctrl.CharacterUIEntrance;
				_fighterVFX.opacity               = _ctrl.FightersOpacity;
			}

			// Update styling of notification stacks
			// ----------------------------------------
			foreach (CharEntry @char in _entries)
			{
				var stack = @char.notifications;

				for (var i = 0; i < stack.activeNotifs.Count; i++)
				{
					var entry = stack.activeNotifs[i];

					if (i == 0) // First
					{
						entry.targetAlpha = 1f;
						entry.targetScale = 1f;
					}
					else // Everything after
					{
						entry.targetAlpha = 0.75f;
						entry.targetScale = 0.75f;
					}
				}
			}
		}

		private void InitDistribution()
		{
			enableDistribution     = true;
			distributionDuration   = 2;
			xpTicker.ratePerSecond = xpTicker.total / distributionDuration;
			rpTicker.ratePerSecond = rpTicker.total / distributionDuration;
		}

		public async UniTask Play(ArenaVictoryController ctrl)
		{
			// Init
			// ----------------------------------------
			_ctrl   = ctrl;
			ctrl.ui = this;

			_fighterVFX = new ManualVFX();
			if (fighters != null)
			{
				foreach (Fighter fighter in fighters)
				{
					fighter.actor.vfx.Add(_fighterVFX);
				}
			}

			// Play intro
			PlayIntro(ctrl);

			// Wait until we start distributing.
			while (!enableDistribution)
				await UniTask.NextFrame();

			InitDistribution();

			await DistributeAsync();
			await ConfirmToContinueAsync();
			await ctrl.Director.ResumeAsync();

			ClearCharacters();

			// Transfer the screen fade opacity to the global state
			ctrl.GetComponent<ScreenFadeAnimator>().Opacity = 0;
			GameEffects.screenFadeOpacity.value             = 1;

			// Reset
			// ----------------------------------------
			_ctrl              = null;
			enableDistribution = false;
		}

		private async Task ConfirmToContinueAsync()
		{
			Label_PressAnyToContinue.gameObject.SetActive(true);
			while (true)
			{
				if (GameInputs.confirm.IsPressed)
					break;

				await UniTask.NextFrame();
			}

			foreach (CharEntry entry in _entries)
			{
				entry.loopingTickParticles.SetActive(false);
			}

			Label_PressAnyToContinue.gameObject.SetActive(false);
		}

		private async Task DistributeAsync()
		{
			var fastforward = false;

			foreach (CharEntry entry in _entries)
			{
				entry.loopingTickParticles.SetActive(true);
				entry.loopingTickParticles.SetPlaying(true);
			}

			Dictionary<CharEntry, TickerValue> xpTickers = new Dictionary<CharEntry, TickerValue>();
			Dictionary<CharEntry, TickerValue> rpTickers = new Dictionary<CharEntry, TickerValue>();
			int shareAmong = _entries.Count;
			foreach (CharEntry @char in _entries)
			{
				xpTickers.Add(@char, new TickerValue(1 + xpTicker.total / shareAmong) { ratePerSecond = xpTicker.ratePerSecond });
				rpTickers.Add(@char, new TickerValue(1 + rpTicker.total / shareAmong) { ratePerSecond = rpTicker.ratePerSecond });
			}

			while (!FinishedIncomeDistribution)
			{
				foreach (CharEntry @char in _entries)
				{
					@char.ui.UpdateIncome(
						xpTickers[@char],
						rpTickers[@char],
						ShowLevelUp,
						ShowMasteryUp);
				}

				xpTicker.remaining = xpTickers.Values.Select(x => x.remaining).Aggregate(0f, (x,y) => x + y);
				rpTicker.remaining = rpTickers.Values.Select(x => x.remaining).Aggregate(0f, (x,y) => x + y);

				// Refresh labels Labels
				RefreshTotalLabels();
				// Label_TotalCredits.Text = rpTick.ToString();

				// SFX pitch
				_incomeTickPitch = Mathf.Clamp(_incomeTickPitch + IncomeSfxPitchChange, 0, IncomeSfxPitchMax);


				// Note(C.L. 8-14-22): Removed due to infinite loop

				/*if (!fastforward && GameInputs.confirm.IsPressed)
					fastforward = true;

				if (!fastforward)
				{

				}*/

				// SFX
				GameSFX.PlayGlobal(IncomeTickSFX, _incomeTickPitch);
				await UniTask.NextFrame();
			}

			foreach (LootEntry loot in itemLoots)
			{
				loot.AwardAll();
			}

			foreach (CharEntry entry in _entries)
			{
				entry.loopingTickParticles.SetPlaying(false);
			}
		}

		public void ShowLevelUp([NotNull] VictoryCharacterUI ui)
		{
			CharEntry @char = _entries[ui.index];

			NotificationRectStack stack = @char.notifications;
			NotificationRectStack.Notif notif = stack.PushPrefab(new NotificationRectStack.Notif
			{
				prefab = LevelUpPanelPrefab,
				id     = ui.character,
			});

			LevelUpPanel panel = notif.go.GetComponent<LevelUpPanel>();
			panel.SetValue1(ui.character.Level);
			panel.Enter();

			GameSFX.PlayGlobal(LevelUpSFX);
			Instantiate(FX_LevelUpPrefab, @char.actorObject.transform.position, Quaternion.identity).SetAutoDestroyPS();
		}

		public void ShowMasteryUp([NotNull] VictoryCharacterUI ui, LimbInstance limb)
		{
			CharEntry @char = _entries[ui.index];

			NotificationRectStack stack = @char.notifications;
			NotificationRectStack.Notif notif = stack.PushPrefab(new NotificationRectStack.Notif
			{
				prefab = MasteryUpPanelPrefab,
				id     = limb,
			});

			SkillAsset unlockedSkill = limb.FindUnlockedSkills().LastOrDefault();

			LevelUpPanel panel = notif.go.GetComponent<LevelUpPanel>();
			panel.SetLimb(limb.Asset).ForgetWithErrors();
			panel.SetValue1(limb.Mastery);

			panel.SetValue2(unlockedSkill != null
				? $"{unlockedSkill.name} Learned!"
				: "No skill learned.");
			panel.Enter();

			foreach (CharEntry entry in _entries)
			foreach (var n in entry.notifications.activeNotifs)
			{
				LevelUpPanel pan = n.go.GetComponent<LevelUpPanel>();
				pan.FastforwardEntrance();
			}


			GameSFX.PlayGlobal(MasteryUpSFX);

			Instantiate(FX_MasteryUpPrefab, @char.actorObject.transform.position, Quaternion.identity).SetAutoDestroyPS();
		}

		private static void PlayIntro([NotNull] ArenaVictoryController ctrl)
		{
			PlayableDirector director = ctrl.Director;
			director.AjPlay(ctrl.Timeline);
		}

		[Button]
		private void TestLevelUp()
		{
			ShowLevelUp(_entries.Choose().ui);
		}

		[Button]
		private void TestMasteryUp()
		{
			VictoryCharacterUI ui = _entries.Choose().ui;
			ShowMasteryUp(ui, ui.character.Limbs.Choose());
		}


		public class TickerValue
		{
			public int total;
			public int numDigits;

			public float remaining;
			public float ratePerSecond;

			public TickerValue(int total)
			{
				this.total    = total;
				numDigits     = total == 0 ? 1 : Mathf.FloorToInt(Mathf.Log10(total)) + 1;
				remaining     = total;
				ratePerSecond = 0;
			}

			public override string ToString()
			{
				return ((int)remaining).ToString().PadLeft(numDigits, '0');
			}
		}
	}
}