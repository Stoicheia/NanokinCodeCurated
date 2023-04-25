using System;

namespace Anjin.Util
{
	public static partial class Extensions
	{
		public static float NextFloat(this Random rand) => (float) rand.NextDouble();
	}
}