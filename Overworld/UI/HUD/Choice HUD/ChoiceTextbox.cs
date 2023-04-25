using System;
using System.Collections.Generic;
using Anjin.EditorUtility;
using Anjin.Nanokin;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Util;
using Util.Odin.Attributes;
using System.Linq;

namespace Anjin.UI
{
	public class ChoiceTextbox : SerializedMonoBehaviour
	{
		[ShowInPlay]
		private List<ChoiceTextboxItem> ActiveItems;

		public ChoiceTextboxItem                prefab;
		public ComponentPool<ChoiceTextboxItem> ItemPool;
		public RectTransform                    itemContainer;
		public ScrollRect                       ScrollRect;

		public HUDElement Element;

		[NonSerialized] public bool        IsActive;
		[NonSerialized] public bool        Dislaying;
		[NonSerialized] public Action<int> OnConfirm;

		private void Awake()
		{
			ItemPool          = new ComponentPool<ChoiceTextboxItem>(transform, prefab);
			ItemPool.initSize = 20;

			ActiveItems = new List<ChoiceTextboxItem>();

			GameInputs.DeviceChanged += device =>
			{
				if (device == InputDevices.KeyboardAndMouse)
				{
					EventSystem.current.SetSelectedGameObject(null);
				}
			};

			Element.Alpha = 0;
		}

		[Button]
		public void Test()
		{
			Show(new List<string>
			{
				"This is a test choice.",
				"This is another test choice",
				"Blah Blah Blah",
				"Call us the crime sweepers, cause we’re on the case!",
				"Sorry, we uh… Gotta go do… Dishes. Or something.",
				"Sorry, we uh… Gotta go do… Dishes. Or something.",
				"Sorry, we uh… Gotta go do… Dishes. Or something.",
				"Sorry, we uh… Gotta go do… Dishes. Or something.",
				"Sorry, we uh… Gotta go do… Dishes. Or something.",
				"Sorry, we uh… Gotta go do… Dishes. Or something.",
			}, new List<bool> {
				false,
				false,
				false,
				false,
				false,
				false,
				false,
				false,
				false,
				true
			}, i => Debug.Log("Selected " + i));
		}

		private void Update()
		{
			// If showing
			if (ActiveItems.Count > 0)
			{
				if (GameInputs.move.AnyPressed &&
				    EventSystem.current.currentSelectedGameObject == null)
				{
					EventSystem.current.SetSelectedGameObject(ActiveItems[0].gameObject);

					MoveDirection move_dir = MoveDirection.None;

					if (GameInputs.move.up.IsPressed) move_dir   = MoveDirection.Up;
					if (GameInputs.move.down.IsPressed) move_dir = MoveDirection.Down;

					ExecuteEvents.Execute(
						ActiveItems[0].gameObject,
						new AxisEventData(EventSystem.current) {moveDir = move_dir},
						ExecuteEvents.moveHandler);

					ScrollTo(ActiveItems[0].rt);
				}

				if (GameInputs.confirm.IsPressed)
				{
					if (EventSystem.current.currentSelectedGameObject.TryGetComponent(out ChoiceTextboxItem item))
					{
						ConfirmItem(item);
					}
				}

				if (GameInputs.cancel.IsPressed)
				{
					var cancelChoices = ActiveItems.Where(x => x.cancelChoice).ToList();

					ChoiceTextboxItem item = (((cancelChoices != null) && (cancelChoices.Count > 0)) ? cancelChoices[0] : ActiveItems[0]);
					ConfirmItem(item);
				}
			}
		}

		public void Show(List<string> text, List<bool> cancels, Action<int> onConfirm)
		{
			IsActive = true;
			_show(text, cancels, onConfirm);
		}

		async void _show(List<string> text, List<bool> cancels, Action<int> onConfirm)
		{
			if (text == null) return;

			if (Dislaying)
				await Hide();

			Dislaying = true;

			GameInputs.mouseUnlocks.Set("choice_hud", true);

			for (int i = 0; i < text.Count; i++) {

				bool cancel = false;
				if (cancels.TryGet(i, out bool c))
					cancel = c;

				var  item   = SpawnTextbox(text[i], cancel);
				if (item == null) continue;

				item.index = i;

				item.onPointerUp = ConfirmItem;
				item.onSelected += tb =>
				{
					ScrollTo(tb.rt);
				};
			}

			await UniTask.DelayFrame(2);

			UpdateUI();

			if (ActiveItems.Count > 0) {
				EventSystem.current.SetSelectedGameObject(ActiveItems[0].gameObject);
				ActiveItems[0].ShowHighlight();
			}

			Element.DoOffset(new Vector3(0, -30), Vector3.zero, 0.25f);
			await Element.DoAlphaFade(1, 0.15f).Tween.ToUniTask();

			OnConfirm = onConfirm;
		}

		public async UniTask Hide(bool stopActive = true)
		{
			if (!Dislaying) return;
			OnConfirm = null;

			GameInputs.mouseUnlocks.Set("choice_hud", false);

			Element.DoOffset(Vector3.zero, new Vector3(0, -30), 0.25f);
			await Element.DoAlphaFade(0, 0.15f).Tween.ToUniTask();

			ClearLines();

			Dislaying = false;
			if (stopActive == true) IsActive = false;
		}

		ChoiceTextboxItem SpawnTextbox(string text, bool cancel)
		{
			ChoiceTextboxItem item = ItemPool.Rent();

			item.text.text = text;
			item.cancelChoice = cancel;

			var rot = item.rt.localRotation;
			item.rt.SetParent(itemContainer, false);
			item.rt.localRotation = rot;
			item.HideHighlight();

			if (ActiveItems == null)
				ActiveItems = new List<ChoiceTextboxItem>();
			ActiveItems.Add(item);

			return item;
		}

		public void ClearLines()
		{
			foreach (ChoiceTextboxItem item in ActiveItems)
			{
				var rot = item.rt.localRotation;
				ItemPool.ReturnSafe(item);
				item.rt.localRotation = rot;
			}

			ActiveItems.Clear();
		}

		public void ConfirmItem(ChoiceTextboxItem item)
		{
			OnConfirm?.Invoke(item.index);
			Hide();
		}

		public void UpdateUI()
		{
			UGUI.SetupListNavigation(ActiveItems, AxisDirection.Vertical);
		}

		public void ScrollTo(RectTransform target)
		{
			Canvas.ForceUpdateCanvases();
			ScrollRect.ScrollTo(target);
		}
	}
}