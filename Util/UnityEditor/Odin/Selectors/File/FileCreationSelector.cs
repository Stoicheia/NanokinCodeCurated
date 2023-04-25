#if UNITY_EDITOR
using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;

namespace Util.Odin.Selectors.File
{
	public class FileCreationSelector : OdinSelector<FileCreationSelectorEntry>
	{
		private readonly IEnumerable<FileCreationSelectorEntry> _entries;

		public FileCreationSelector(IEnumerable<FileCreationSelectorEntry> entries)
		{
			_entries = entries;
		}

		protected override void BuildSelectionTree(OdinMenuTree tree)
		{
			tree.Config.DrawSearchToolbar = true;
			tree.AddRange(_entries, entry => entry.Label).AddThumbnailIcons();
			EnableSingleClickToSelect();
		}
	}
}
#endif