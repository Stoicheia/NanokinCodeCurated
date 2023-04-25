using System;
using System.Collections.Generic;
using Anjin.Nanokin;
using Anjin.Util;
using Data.Overworld;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using UnityUtilities;
using Util.Addressable;
using Util.Odin.Attributes;

namespace Menu.Quest
{
	//[ExecuteAlways]
	public class QuestCard : SelectableExtended<QuestCard>, IPointerClickHandler {
		public enum Mode { Normal, Complete, Failed }

		[SerializeField] public Image           Image;
		[SerializeField] public TextMeshProUGUI Label;
		[SerializeField] public TextMeshProUGUI Number;
		[SerializeField] public Selectable      Selectable;

		public Image BG_Normal;
		public Image BG_Complete;
		public Image BG_Failed;

		public TextMeshProUGUI TMP_Completed;
		public TextMeshProUGUI TMP_Failed;
		public TextMeshProUGUI TMP_Type;

		public List<Image>     Pips;
		public List<Image>     Lines;
		public List<Image>     IncompletePips;

		[NonSerialized] public Animation          animator;
		[NonSerialized] public Quests.LoadedQuest quest;

		[NonSerialized, ShowInPlay] public int  index;
		[NonSerialized, ShowInPlay] public bool deathScheduled;

		[NonSerialized] public RectTransform rect;

		[NonSerialized] public Areas area;

		public event Action<QuestCard> Clicked;

		private void Awake()
		{
			animator = GetComponent<Animation>();
			rect     = GetComponent<RectTransform>();

			UpdatePips(0, 0);
			UpdateMode(Mode.Normal);
		}

		private void LateUpdate()
		{
			if (deathScheduled && !animator.isPlaying)
			{
				Destroy(gameObject); // TODO recycle into prefab pool instead
			}
		}

		public void OnPointerClick(PointerEventData eventData)
		{
			Clicked?.Invoke(this);
		}

		public void UpdateType(bool visible, QuestType type)
		{
			if(visible) {
				TMP_Type.gameObject.SetActive(true);
				TMP_Type.text = type.ToString();
			} else {
				TMP_Type.gameObject.SetActive(false);
			}
		}

		public async void UpdateSprite(string address)
		{
			if (!Image) return;

			Image.color = Image.color.Alpha(1);
			AsyncOperationHandle<Sprite> handle = await Addressables2.LoadHandleAsync<Sprite>(address);
			if (handle.Result != null) {
				Image.sprite = handle.Result;
				Image.color = Image.color.Alpha(1);
			}
		}

		[Button]
		public void UpdateMode(Mode mode)
		{
			BG_Normal.gameObject.SetActive(false);
			BG_Complete.gameObject.SetActive(false);
			BG_Failed.gameObject.SetActive(false);

			TMP_Completed.gameObject.SetActive(false);
			TMP_Failed.gameObject.SetActive(false);

			if (Image) Image.color.Alpha(1);

			switch (mode) {
				case Mode.Normal:
					BG_Normal.gameObject.SetActive(true);
					targetGraphic = BG_Normal;
					break;

				case Mode.Complete:
					BG_Complete.gameObject.SetActive(true);
					TMP_Completed.gameObject.SetActive(true);
					targetGraphic = BG_Complete;
					break;

				case Mode.Failed:
					BG_Failed.gameObject.SetActive(true);
					TMP_Failed.gameObject.SetActive(true);
					targetGraphic = BG_Failed;
					if (Image) Image.color.Alpha(0.4f);
					break;
			}
		}

		[Button]
		public void UpdatePips(int visible, int completed)
		{
			for (int i = 0; i < Pips.Count; i++) 			Pips[i].gameObject.SetActive(false);
			for (int i = 0; i < Lines.Count; i++) 			Lines[i].gameObject.SetActive(false);
			for (int i = 0; i < IncompletePips.Count; i++) 	IncompletePips[i].color = IncompletePips[i].color.Alpha(i < visible ? 1 : 0);

			for (int i = 0; i < visible && i < Pips.Count && i < Lines.Count; i++) {
				Pips[i].gameObject.SetActive(true);

				if(i < visible - 1)
					Lines[i].gameObject.SetActive(true);

				if (i < completed) {
					Pips[i].color  = Pips[i].color.Alpha(1);
					IncompletePips[i].color = IncompletePips[i].color.Alpha(0);
				} else {
					Pips[i].color  = Pips[i].color.Alpha(0.2f);
				}

				if (i < completed - 1) {
					Lines[i].color = Lines[i].color.Alpha(1);
				} else {
					Lines[i].color = Lines[i].color.Alpha(0.2f);
				}
			}
		}

		protected override QuestCard Myself => this;
	}
}