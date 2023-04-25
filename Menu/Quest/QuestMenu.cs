using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.EditorUtility;
using Anjin.Nanokin;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using Data.Overworld;
using JetBrains.Annotations;
using Puppets.Render;
using SaveFiles;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Util.Extensions;

namespace Menu.Quest
{
	public class QuestMenu : StaticMenu<QuestMenu>
	{
		[Title("Prefabs")]
		[SerializeField] private QuestCard MapCardPrefab;
		[SerializeField]                                                private QuestCard        QuestCardPrefab;
		[FormerlySerializedAs("DetailObjectivePrefab"), SerializeField] private QuestObjectiveUI DetailObjectiveUIPrefab;

		[Title("References")]
		[SerializeField] private Animation Animator;
		[SerializeField] private SplicerTabBar TabBar;
		[SerializeField] private Areas[]       TabValues;
		[SerializeField] private Transform     Root_Cards;
		[SerializeField] private Transform     Root_CardsNoLayout;
		[SerializeField] private ScrollRect    CardsScrollRect;
		public Button BackButton;
		//[SerializeField] private EventTrigger  ClickpaneDetailFullscreen;
		//[SerializeField] private EventTrigger  ClickpaneQuestlist;


		[Title("Detail Pane", HorizontalLine = false)]
		[SerializeField] private TextMeshProUGUI TMP_DetailTitle;
		[SerializeField] private TextMeshProUGUI TMP_DetailDescription;
		[SerializeField] private TextMeshProUGUI TMP_DetailProgress;
		[SerializeField] private TextMeshProUGUI TMP_DetailLocation;
		[SerializeField] private Transform       Root_DetailObjectives;

		[Title("Animations")]
		[SerializeField] private AnimationClip CardAppear;
		[SerializeField] private AnimationClip CardDisappear;
		[SerializeField] private AnimationClip CardConfirm;
		[SerializeField] private AnimationClip CardActivate;
		[SerializeField] private AnimationClip CardDeactivate;
		[SerializeField] private AnimationClip DetailsAppear;
		[SerializeField] private AnimationClip DetailsDisappear;

		private List<QuestEntry.Mirror> _allQuests;

		private States                        _state      = States.None;
		private List<QuestCard>               _cards      = new List<QuestCard>();
		private List<QuestObjectiveUI>        _objectives = new List<QuestObjectiveUI>();
		private Dictionary<Areas, SplicerTab> _areasToTabs = new Dictionary<Areas, SplicerTab>();

		private int                    _selectedPage;
		private Areas                  _pageArea;
		private QuestCard              _selectedCard;

		protected override void OnAwake()
		{
			base.OnAwake();

			_allQuests        = new List<QuestEntry.Mirror>();
			_cards            = new List<QuestCard>();
			//_questIconHandles = new AsyncHandles();
		}

		protected override void Start()
		{
			// tab changed, update the cards
			TabBar.SelectionChanged += (prev, next) =>
			{
				for (var i = 0; i < TabBar.tabs.Length; i++)
				{
					SplicerTab tab = TabBar.tabs[i];
					tab.SetActive(i == next);
				}

				if (prev == next && _state == States.QuestList)
					// No change
					return;

				Areas selectedValue = Areas.None;

				if(next > 0 && next < 10)
					selectedValue = TabValues[next - 1];

				_selectedPage = next;
				_pageArea     = selectedValue;

				_selectedCard   = null;
				RefreshCardList();
				RefreshUI();

				//ChangeState(States.QuestList);
			};

			Quests.Live.OnQuestsChanged += () => {
				if (!menuActive) return;
				_selectedCard = null;
				RefreshCardList(false);
				RefreshUI();
			};

			BackButton.onClick.AddListener(OnExitStatic);


			// Keep this at the end!
			base.Start();
		}

		protected override async UniTask enableMenu()
		{
			GameInputs.forceUnlocks.Add("quest_menu");

			_allQuests.Clear();
			_allQuests.AddRange(await UniTask.WhenAll(SaveManager.current.Quests.Select(q => q.ToMirrorAsync()).ToList()));

			Animator.PlayClip(DetailsAppear);

			// Enable area tabs with quests in them (relevant)
			// -----------------------------------------------
			var numRelevant  = 0;
			var relevancyMap = new bool[TabValues.Length];

			foreach (SplicerTab tab in TabBar.tabs)
			{
				tab.gameObject.SetActive(true);
			}

			// Needs to be done before RefreshCardList()
			_areasToTabs.Clear();
			for (int i = 0; i < TabValues.Length; i++) {
				_areasToTabs[TabValues[i]] = TabBar.tabs[i + 1];
			}

			TabBar.SelectFirstAvailable();

			RefreshCardList();
			RefreshUI();
		}

		protected override UniTask disableMenu()
		{
			//_questIconHandles.ReleaseAll();
			GameInputs.forceUnlocks.Remove("quest_menu");

			/*foreach (SaveData.Quest.Mirror allQuest in _allQuests)
			{
				Addressables2.Release(allQuest.handle);
			}*/

			return UniTask.CompletedTask;
		}

		private void Update()
		{
			if (GameInputs.menuLeft.IsPressed) {
				PrevPage();
			} else if (GameInputs.menuRight.IsPressed) {
				NextPage();
			}

			// Handle no card being selected (e.g. when player clicks elsewhere on the screen)
			// We start back from the currently equipped limb
			if (GameInputs.menuNavigate.AnyPressed &&
				EventSystem.current.currentSelectedGameObject == null && _cards.Count > 0)
			{

				MoveDirection move_dir = MoveDirection.None;

				if (GameInputs.menuNavigate.left.IsPressed) move_dir  = MoveDirection.Left;
				if (GameInputs.menuNavigate.right.IsPressed) move_dir = MoveDirection.Right;
				if (GameInputs.menuNavigate.up.IsPressed) move_dir    = MoveDirection.Up;
				if (GameInputs.menuNavigate.down.IsPressed) move_dir  = MoveDirection.Down;


				if (_selectedCard != null)
					_selectedCard.Select();
				else
					_selectedCard = _cards[0];

				ExecuteEvents.Execute(
					_selectedCard.gameObject,
					new AxisEventData(EventSystem.current) {moveDir = move_dir},
					ExecuteEvents.moveHandler);
			}

			if (GameInputs.cancel.IsPressed /*&& _state == States.MapList*/)
			{
				DoExitControls();
			}
		}

		private void NextPage()        => TabBar.SelectNextAvailable();
		private void PrevPage()        => TabBar.SelectPreviousAvailable();
		private void SelectPage(int i)
		{
			if(i != _selectedPage)
				TabBar.Select(i, true);
		}

		private void DisappearCards(bool anim)
		{
			// Disappear the existing cards
			// ----------------------------------------
			for (var i = 0; i < _cards.Count; i++)
			{
				QuestCard card = _cards[i];

				Vector3 opos = card.transform.position;
				card.transform.parent   = Root_CardsNoLayout;
				card.transform.position = opos;

				if(anim)
					card.animator.PlayClip(CardDisappear);

				card.deathScheduled = true;
			}

			_cards.Clear();
		}

		private void OpenQuestDetails(QuestCard nextCard)
		{
			if (_selectedCard != nextCard)
			{
				if (_selectedCard != null)
				{
					_selectedCard.animator.PlayClip(CardDeactivate);
				}

				nextCard.animator.PlayClip(CardActivate);
			}

			_selectedCard = nextCard;
			RefreshUI();
		}

		private UniTask ChangeState(States next)
		{
			if (next == _state)
			{
				RefreshUI();
				return UniTask.CompletedTask;
			}

			_state = next;

			// ENTER NEXT STATE
			// ----------------------------------------
			switch (next)
			{
				case States.None:
					break;

				case States.QuestList:
					// Create a card for each quest
					// ----------------------------------------
					/*foreach (Quests.LoadedQuest entry in Quests.LoadedQuests) {

						if (entry.state == QuestState.NotStarted)
							continue;
						/*QuestAsset asset = entry.asset;
						if (asset.Level != _pageLevel)
							continue;#1#

						QuestCard card = Instantiate(QuestCardPrefab, Root_Cards);
						card.quest = entry;

						card.Label.text = entry.GetName();
						card.animator.Play(CardAppear);
						card.Clicked += OpenQuestDetails;

						_cards.Add(card);
					}*/

					// TODO: Card images?
					/*async UniTask LoadSprite(QuestCard card)
					{
						if (card.asset.Sprite.IsValid())
						{
							card.Image.sprite = await _questIconHandles.LoadAssetAsync(card.asset.Sprite);
						}
					}

					UniTask.WhenAll(_cards.Select(LoadSprite).ToList());*/
					break;

				/*case States.QuestDetail:
					Animator.Play(DetailsAppear);
					break;
					*/

				default:
					throw new ArgumentOutOfRangeException(nameof(next), next, null);
			}

			RefreshUI();
			return UniTask.CompletedTask;
		}

		private List<Quests.LoadedQuest> _scratchQuests = new List<Quests.LoadedQuest>();

		private void RefreshCardList(bool anim = true)
		{
			DisappearCards(anim);

			bool  all  = _selectedPage == 0;
			bool  misc = _selectedPage == TabBar.tabs.Length - 1;

			_scratchQuests.Clear();
			_scratchQuests.AddRange(Quests.LoadedQuests);
			_scratchQuests.Sort((x, y) => {

				int failed = 0;

				if (x.is_failed  && !y.is_failed) failed = 1;
				if (!x.is_failed && y.is_failed)  failed = -1;

				if (failed != 0) return failed;

				int finished = 0;

				if (x.is_finished  && !y.is_finished) 	finished = 1;
				if (!x.is_finished && y.is_finished) 	finished = -1;

				if (finished != 0) return finished;

				int type = 0;

				if (x.type == QuestType.Story && y.type == QuestType.Sub) type = -1;
				if (x.type == QuestType.Sub && y.type == QuestType.Story) type = 1;

				if(type != 0) return type;

				return x.GetName().CompareTo(y.GetName());
			});

			for (int i = 1; i < TabValues.Length + 1; i++)
				TabBar.tabs[i].gameObject.SetActive(false);

			/*foreach (SplicerTab tab in tab) {
				tab.gameObject.SetActive(false);
			}*/
			//TabBar.tabs[0].SetActive();

			/*foreach (Quests.LoadedQuest entry in _scratchQuests) {

			}*/

			foreach (Quests.LoadedQuest entry in _scratchQuests)
			{
				if (entry.state == QuestState.NotStarted && !Quests.Live.Debug_ShowAllInTracker)
					continue;

				if (_areasToTabs.TryGetValue(entry.area, out var tab) && !tab.gameObject.activeSelf)
					tab.gameObject.SetActive(true);

				if (!(all || entry.area == _pageArea || entry.area == Areas.None && misc))
					continue;

				QuestCard card = Instantiate(QuestCardPrefab, Root_Cards);
				card.quest = entry;
				card.area  = entry.area;

				card.Label.text = entry.GetName();

				if(anim)
					card.animator.PlayClip(CardAppear);

				card.Clicked += OpenQuestDetails;


				card.UpdatePips(entry.phases.Count, entry.current_phase);
				card.UpdateType(true, entry.type);

				if(entry.image_address != null)
					card.UpdateSprite(entry.image_address);

				// TODO: Failed quests
				if(entry.is_finished)
					card.UpdateMode(QuestCard.Mode.Complete);
				else if(entry.is_failed)
					card.UpdateMode(QuestCard.Mode.Failed);
				else
					card.UpdateMode(QuestCard.Mode.Normal);

				_cards.Add(card);
			}

			UGUI.SetupGridNavigation(_cards, 3);

			foreach (QuestCard card in _cards) {
				card.onSelected += OpenQuestDetails;
			}



			// Select the equipped limb.
			if(_cards.Count > 0) {
				if (_selectedCard != null)
					SelectCard(_selectedCard);
				else
					SelectCard(_cards[0]);
			}
		}

		public void SelectCard([NotNull] QuestCard card)
		{
			/*if (EventSystem.current.alreadySelecting)
				GameSFX.PlayGlobal(SFX_SelectCard, this);
			else*/
				EventSystem.current.SetSelectedGameObject(card.gameObject);

			ScrollTo(card.rect);
		}

		private void RefreshUI()
		{
			// Destroy old objectives
			foreach (QuestObjectiveUI ui in _objectives)
			{
				Destroy(ui.gameObject);
			}

			_objectives.Clear();

			if (_selectedCard == null) {
				TMP_DetailTitle.text       = "";
				TMP_DetailDescription.text = "";
				TMP_DetailProgress.gameObject.SetActive(false);
				TMP_DetailLocation.gameObject.SetActive(false);
				return;
			}

			if (_selectedCard.quest.is_finished) {
				TMP_DetailTitle.text       = _selectedCard.quest.GetName();
				TMP_DetailDescription.text = "TODO: Finished Quests";
				TMP_DetailProgress.gameObject.SetActive(false);
				return;
			}

			Quests.PhaseDescription description = _selectedCard.quest.phases[_selectedCard.quest.current_phase].baked_description;
			Quests.LoadedQuest      quest       = description.quest;

			TMP_DetailTitle.text       = _selectedCard.quest.GetName();
			if(!description.description.IsNullOrWhitespace())
				TMP_DetailDescription.text = description.description;
			else
				TMP_DetailDescription.text = _selectedCard.quest.description;

			if (description.has_progress) {
				TMP_DetailProgress.gameObject.SetActive(true);
				TMP_DetailProgress.text = description.progress_label + $" {description.progress_current} / {description.progress_goal}";
			} else {
				TMP_DetailProgress.gameObject.SetActive(false);
			}

			if (quest.area != Areas.None || quest.sub_area != LevelID.None) {
				TMP_DetailLocation.gameObject.SetActive(true);
				TMP_DetailLocation.text = $"{quest.sub_area.ToString()}, {quest.area.ToString()}";

			} else {
				TMP_DetailLocation.gameObject.SetActive(false);
			}

			// TODO: Info printout
			// Create new objectives
			for (var i = 0; i < description.objectives.Count; i++)
			{
				Quests.ObjectiveDescription obj = description.objectives[i];

				QuestObjectiveUI ui  = Instantiate(DetailObjectiveUIPrefab, Root_DetailObjectives);

				ui.TMP_Label.text      = obj.name;
				ui.TMP_Label.fontStyle = obj.completed ? FontStyles.Strikethrough	: FontStyles.Normal;
				ui.TMP_Label.color     = obj.completed ? ui.Col_Complete			: ui.Col_Active;

				ui.Root_Checkbox.SetActive(obj.completed);

				// Update completion UI
				/*ui.Root_Checkbox.gameObject.SetActive(obj.Quantity == 0);
				ui.Root_Quota.gameObject.SetActive(obj.Quantity > 0);
				if (obj.Quantity > 0)
				{
					ui.TMP_QuotaMin.text = _detailCard.entry.Objectives.Count > i ? _detailCard.entry.Objectives[i].Progress.ToString() : "??";
					ui.TMP_QuotaMax.text = obj.Quantity.ToString();
				}*/

				LayoutRebuilder.ForceRebuildLayoutImmediate(ui.Root_Checkbox.GetComponent<RectTransform>());
				LayoutRebuilder.ForceRebuildLayoutImmediate(ui.Root_Quota.GetComponent<RectTransform>());

				_objectives.Add(ui);
			}

			/*if (_state == States.QuestDetail)
			{

			}*/
		}

		public void ScrollTo(RectTransform target)
		{
			Canvas.ForceUpdateCanvases();
			UGUI.ScrollTo(target);
			CardsScrollRect.ScrollTo(target);
		}

		public enum States
		{
			None,
			//MapList,
			QuestList,
			//QuestDetail
		}
	}
}