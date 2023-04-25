using System;
using System.Collections.Generic;
using JetBrains.Annotations;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace Util.Odin.Selectors.Callback
{
#if UNITY_EDITOR
	public class CallbackSelector : OdinSelector<CallbackSelectorEntry>
	{
		private List<CallbackSelectorEntry> _entries;
		private OdinEditorWindow            _window;

		public CallbackSelector(IEnumerable<CallbackSelectorEntry> entries)
		{
			_entries = new List<CallbackSelectorEntry>(entries);
		}

		public CallbackSelector(params CallbackSelectorEntry[] entries)
		{
			_entries = new List<CallbackSelectorEntry>(entries);
		}

		public CallbackSelector AddEntry([NotNull] string label, [NotNull] Action onPicked)
		{
			_entries.Add(new CallbackSelectorEntry(label, onPicked));
			return this;
		}

		protected override void BuildSelectionTree(OdinMenuTree tree)
		{
			tree.Config.DrawSearchToolbar = true;
			tree.AddRange(_entries, entry => entry.Label).AddThumbnailIcons();
			EnableSingleClickToSelect();
			SelectionConfirmed += entries =>
			{
				// _window.Close(); // Close NOW because we may want to do a nested submenu which may require opening another window as popup, and it fucks with 2 windows apparently.
				// edit: closing early here causes an error. Although it doesn't break anything, may have to report this issue to Odin because it's really annoying.
				foreach (CallbackSelectorEntry entry in entries)
				{
					entry.PickHandler();
				}
			};
		}

		private void ShowContextMenu(float w)
		{
			_window = ShowInPopup(w);
		}

		public static void Show(params CallbackSelectorEntry[] entries)
		{
			Show(200, entries);
		}

		public static void Show(float w, params CallbackSelectorEntry[] entries)
		{
			new CallbackSelector(entries).ShowContextMenu(w);
		}

		public static void Show(float w, IEnumerable<CallbackSelectorEntry> entries)
		{
			new CallbackSelector(entries).ShowContextMenu(w);
		}
	}
#endif
}