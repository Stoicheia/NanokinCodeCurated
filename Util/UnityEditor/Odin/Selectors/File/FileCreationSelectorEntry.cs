using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Util.Odin.Selectors.File
{
	public class FileCreationSelectorEntry
	{
		public delegate void FileProcessorCallback(TextAsset asset);


		public FileCreationSelectorEntry(string label, Func<Object, string> defaultFileName, Func<string> textContent)
		{
			Label           = label;
			DefaultFileName = defaultFileName;
			TextContent     = textContent;
		}

		public string Label { get; }

		public Func<Object, string> DefaultFileName { get; }

		public Func<string> TextContent { get; }
	}
}