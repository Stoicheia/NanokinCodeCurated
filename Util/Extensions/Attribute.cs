
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Anjin.Util {
	public static partial class Extensions
	{
		/// <summary>
		/// http://stackoverflow.com/a/35040378/1319727
		/// </summary>
		public static TAttribute GetAttribute<TAttribute>(this Enum value) where TAttribute : Attribute
		{
			Type   enumType = value.GetType();
			string name     = Enum.GetName(enumType, value);

			if (name == null)
				return null;

			return enumType.GetField(name).GetCustomAttributes(false).OfType<TAttribute>().SingleOrDefault();
		}
	}
}