using System.Collections.Generic;
using System.Linq;
using Anjin.Nanokin;
using Anjin.Util;
using UnityEngine;
using UnityEngine.InputSystem;
using Util.RenderingElements.Barrel;

namespace Menu.Start
{
	/// <summary>
	/// The scrolling barrel menu of the start menu.
	/// </summary>
	public class StartBarrel : BarrelMenu
	{
		private List<BarrelItem> _items;

		public BarrelItem SelectedItem => _items[SmartSelectionIndex];

		private void Awake()
		{
			// Create the looping panel wheel buffer. (all the panels)
			EnqueueLoopingBuffer<IconTextPanel>();
			FlushAdd();

			foreach (ListPanel panel in AllPanels)
			{
				panel.MoveToEndPosition();
				panel.gameObject.SetActive(false); // Hide the panel initially.
			}
		}

		public void ChangeEntries(List<BarrelItem> list)
		{
			list.RemoveNulls();

			_items = list;

			foreach (ListPanel panel in AllPanels)
			{
				panel.gameObject.SetActive(true);
				panel.MoveToEndPosition();
			}

			UseSlots(list.Count);
			SelectAt(SmartSelectionIndex);
		}

		public override void UpdateBarrelPanel(int index, ListPanel panel, bool isUsed)
		{
			IconTextPanel icPanel = (IconTextPanel)panel;
			BarrelItem    item    = _items.SafeGet(index);

			panel.Value           = item;
			icPanel.Label.Text    = isUsed ? item.text : "";
			icPanel.Label.Strikethrough = isUsed ? !item.selectable : false;
			icPanel.Sprite.Sprite = isUsed ? item.sprite : null;
			icPanel.selectable    = isUsed ? item.selectable : true;
		}

		private void Update()
		{
#if UNITY_EDITOR
			if (GameInputs.IsPressed(Key.Numpad1))
			{
				IconTextPanel panel = Add<IconTextPanel>();

				BarrelItem item = _items.Last();
				panel.Label.Text    = item.text;
				panel.Sprite.Sprite = item.sprite;

				FlushAdd();

				DebugLogger.Log($"There are now {AllPanels.Count} panels in the barrel.", LogContext.UI, LogPriority.Low);
			}
#endif
		}
	}
}