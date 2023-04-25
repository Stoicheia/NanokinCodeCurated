using UnityEngine;

namespace Util.Extensions
{
	public static partial class Extensions
	{
		public static Material Opacity(this Material mat, float? value) => mat.Float("_Opacity", value);
		public static Material Float(this Material mat, string name, float? value)
		{
			if (value == null) value = 0;

			mat.SetFloat(name, value.Value);
			return mat;
		}
	}
}

