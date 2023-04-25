using System;
using System.Linq;
using Anjin.Util;
using Sirenix.OdinInspector;
using Vexe.Runtime.Extensions;

namespace Puppets.Render
{
	public class SplicerTabBar : SerializedMonoBehaviour
	{
		[NonSerialized] public SplicerTab[] tabs;
		[NonSerialized] public int          selectedIndex;
		[NonSerialized] public SplicerTab   selectedTab;

		public event TabChangeHandler SelectionChanged;

		public delegate void TabChangeHandler(int prev, int next);

		private void Awake()
		{
			tabs = GetComponentsInChildren<SplicerTab>();
			for (var i = 0; i < tabs.Length; i++)
			{
				SplicerTab tab = tabs[i];

				tab.tabIndex = i;

				int i1 = i;
				tab.OnClicked += (splicerTab, data) =>
				{
					Select(i1);
				};
			}
		}

		public void SelectFirstAvailable()
		{
			Select(tabs.IndexOf(t => t.gameObject.activeSelf));
		}

		public void Select(int index, bool wrap = false)
		{
			int prev = selectedIndex;

			selectedIndex = wrap ? index.Wrap(tabs.Length) : index.Clamp(tabs);
			selectedTab   = tabs[selectedIndex];

			RefreshUI();
			SelectionChanged?.Invoke(prev, selectedIndex);
		}

		public void SelectNextAvailable()
		{
			for (int i = 0, s = selectedIndex; i < tabs.Length; i++) {
				s++;

				if (s >= tabs.Length)
					s = 0;

				if (tabs[s].gameObject.activeSelf) {
					Select(s, true);
					break;
				}

			}
		}

		public void SelectPreviousAvailable()
		{
			for (int i = 0, s = selectedIndex; i < tabs.Length; i++) {
				s--;

				if (s < 0)
					s = tabs.Length - 1;

				if (tabs[s].gameObject.activeSelf) {
					Select(s, true);
					break;
				}
			}
		}

		public void RefreshUI()
		{
			for (var i = 0; i < tabs.Length; i++)
			{
				SplicerTab tab = tabs[i];
				tab.SetActive(i == selectedIndex);
			}
		}
	}
}