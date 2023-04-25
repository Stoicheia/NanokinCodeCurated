using JetBrains.Annotations;

namespace Util.Odin.Selectors.File
{
	public class FilterEntry<TValue>
	{
		public FilterEntry([NotNull] string label, [NotNull] TValue value)
		{
			Label = label;
			Value = value;
		}

		[NotNull] public string Label { get; }

		[NotNull] public TValue Value { get; }
	}
}