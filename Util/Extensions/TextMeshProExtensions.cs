using JetBrains.Annotations;
using TMPro;

namespace Anjin.Util
{
	public static class TextMeshProExtensions
	{
		// It's not uncommon to wanna overlay several TMPs for a stylized look
		// So this is just a few utility functions to facilitate this
		// ------------------------------------------------------------
		public static void SetTextMulti([NotNull] this TextMeshProUGUI[] tmps, string text)
		{
			for (var i = 0; i < tmps.Length; i++)
			{
				tmps[i].text = text;
			}
		}

		public static void SetTextMulti([NotNull] this TextMeshPro[] tmps, string text)
		{
			for (var i = 0; i < tmps.Length; i++)
			{
				tmps[i].text = text;
			}
		}

		public static void SetTextMulti([NotNull] this TMP_Text[] tmps, string text)
		{
			for (var i = 0; i < tmps.Length; i++)
			{
				tmps[i].text = text;
			}
		}

		public static void SetNumberMulti([NotNull] this TextMeshProUGUI[] tmps, int number)
		{
			for (var i = 0; i < tmps.Length; i++)
			{
				tmps[i].SetText("{0}", number);
			}
		}
	}
}