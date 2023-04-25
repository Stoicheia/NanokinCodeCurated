#if UNITY_EDITOR
using System;
using JetBrains.Annotations;

namespace Util.Odin.Selectors.Callback
{
	public class CallbackSelectorEntry
	{
		public CallbackSelectorEntry([NotNull] string label, [NotNull] Action pickHandler)
		{
			Label       = label;
			PickHandler = pickHandler;
		}

		[NotNull] public Action PickHandler { get; }

		[NotNull] public string Label { get; }
	}
}
#endif