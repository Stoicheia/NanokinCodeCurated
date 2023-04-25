using System;

namespace Anjin.Util
{
	public static partial class Extensions
	{
		public static Enum GetNext(this Enum value)
		{
			int max = Enum.GetNames(value.GetType()).Length;
			return (Enum) Enum.ToObject(value.GetType(), (Convert.ToInt32(value) + 1) % max);
		}

		public static bool HasFlag<E>(this E value, E variable) where E : Enum
		{
			ulong num = Convert.ToUInt64(value);
			return (Convert.ToUInt64(variable) & num) == num;
		}
	}
}