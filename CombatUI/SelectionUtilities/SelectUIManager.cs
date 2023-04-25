using System;

namespace Combat.Components
{
	public struct SelectUIManager
	{
		public int counter;

		/// <summary>
		/// Decides which status UI state to display when none of the
		/// UIs are highlighted.
		/// </summary>
		public SelectUIStates normalState;

		public void Set(ref SelectUIObject obj, bool b)
		{
			if (!obj.state && b)
				counter++;
			else if (obj.state && !b)
				counter--;

			obj.state = b;
		}

		public void Update(ref SelectUIObject obj, ref SelectUIStyle style)
		{
			if (counter == 0)
			{
				obj.state = false;

				switch (normalState)
				{
					case SelectUIStates.Normal:
						obj.brightness = style.NormalBrightness;
						break;

					case SelectUIStates.Dim:
						obj.brightness = style.DimBrightness;
						break;

					case SelectUIStates.Highlight:
						obj.brightness = style.HighlightBrightness;
						break;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			else
			{
				obj.brightness = obj.state
					? style.HighlightBrightness
					: style.DimBrightness;
			}
		}
	}
}