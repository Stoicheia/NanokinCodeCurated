using UnityEngine;
using Util;

namespace Anjin.Util
{
	public static partial class Extensions
	{
		/// <summary>
		/// Get the hexcode of a color
		/// </summary>
		/// <param name="c">The color</param>
		/// <returns>The hexcode in RGBA</returns>
		public static int Hex(this Color c)
		{
			int r = (int) (c.r * 255f);
			int g = (int) (c.g * 255f);
			int b = (int) (c.b * 255f);
			int a = (int) (c.a * 255f);

			return r << 24 | g << 16 | b << 8 | a;
		}

		public static Color Brighten(this Color c, float power = 0.5f)
		{
			return UnityEngine.Color.Lerp(c, UnityEngine.Color.white, power);
		}

		public static Color Darken(this Color c, float power = 0.5f)
		{
			return UnityEngine.Color.Lerp(c, UnityEngine.Color.black, power);
		}

		public static string HexString(this Color c)
		{
			return "#" + ColorUtility.ToHtmlStringRGB(c);
		}

		/// <summary>
		/// Get the RGBA colors in a 0 - 255 range
		/// </summary>
		/// <param name="c">The color</param>
		/// <returns>A Vector4</returns>
		public static Vector4 RGBA(this Color c)
		{
			return new Vector4(c.r * 255f, c.g * 255f, c.b * 255f, c.a * 255f);
		}

		public static Vector4 ToV4(this Color c)
		{
			return new Vector4(c.r, c.g, c.b, c.a);
		}

		public static Color Lerp(this Color c, Color other, float a) => UnityEngine.Color.Lerp(c, other, a);

		public static Color LerpDamp(this Color current, Color final, float damping)
		{
			return new Color(
				MathUtil.LerpDamp(current.r, final.r, damping),
				MathUtil.LerpDamp(current.g, final.g, damping),
				MathUtil.LerpDamp(current.b, final.b, damping),
				MathUtil.LerpDamp(current.a, final.a, damping)
			);
		}

		public static Color32 To32(this Color thisColor)
		{
			byte r = (byte) (thisColor.r * 255);
			byte g = (byte) (thisColor.g * 255);
			byte b = (byte) (thisColor.b * 255);
			byte a = (byte) (thisColor.a * 255);

			return new Color32(r, g, b, a);
		}

		public static Color ScaleAlpha(this Color thisColor, float scalar)
		{
			thisColor.a *= scalar;
			return thisColor;
		}
	}
}