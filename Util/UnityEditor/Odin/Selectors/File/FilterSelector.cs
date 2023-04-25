using System;
using System.Collections.Generic;
using JetBrains.Annotations;

#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
#endif

namespace Util.Odin.Selectors.File
{
#if UNITY_EDITOR
	public class FilterSelector<TValue> : OdinSelector<FilterEntry<TValue>>
	{
		private List<FilterEntry<TValue>>                _entries;
		private OdinEditorWindow                         _window;
		private Action<IEnumerable<FilterEntry<TValue>>> _onSelected;

		public FilterSelector(IEnumerable<FilterEntry<TValue>> entries, Action<IEnumerable<FilterEntry<TValue>>> onSelected)
		{
			_entries    = new List<FilterEntry<TValue>>(entries);
			_onSelected = onSelected;
		}

		public FilterSelector(Action<IEnumerable<FilterEntry<TValue>>> onSelected)
		{
			_entries    = new List<FilterEntry<TValue>>();
			_onSelected = onSelected;
		}

		public FilterSelector<TValue> AddEntry([NotNull] string label, TValue value)
		{
			_entries.Add(new FilterEntry<TValue>(label, value));
			return this;
		}

		protected override void BuildSelectionTree(OdinMenuTree tree)
		{
			tree.Config.DrawSearchToolbar      = true;
			tree.Selection.SupportsMultiSelect = true;
			tree.Config.AutoFocusSearchBar     = true;
			tree.DefaultMenuStyle.Height       = 15;
			tree.DefaultMenuStyle.Borders      = true;
			tree.AddRange(_entries, entry => entry.Label).AddThumbnailIcons();
			EnableSingleClickToSelect();
			SelectionConfirmed += entries =>
			{
				_onSelected(entries);
			};
		}

		private void ShowContextMenu(float w)
		{
			_window = ShowInPopup(w);
		}
	}
#endif
}