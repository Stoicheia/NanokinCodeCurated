using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Anjin.Util
{
	public static partial class Extensions
	{
		/// <summary>
		/// Get all attributes
		/// </summary>
		/// <param name="self"></param>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T[] Attributes<T>(this object self)
		{
			Type    type = self.GetType();
			List<T> att  = new List<T>();
			foreach (var a in type.GetCustomAttributes(typeof(T), false))
			{
				att.Add((T) a);
			}

			return att.ToArray();
		}

		/// <summary>
		/// Do an action based on attributes
		/// </summary>
		/// <param name="self"></param>
		/// <param name="action"></param>
		/// <typeparam name="T"></typeparam>
		public static void Attributes<T>(this object self, Action<T> action)
		{
			T[] att = self.Attributes<T>();
			foreach (var a in att)
			{
				action(a);
			}
		}

		public static R[] Attributes<T, R>(this object self, Func<T, R> func)
		{
			T[] att = self.Attributes<T>();
			R[] res = new R[att.Length];
			for (int i = 0; i < att.Length; i++)
			{
				res[i] = func(att[i]);
			}

			return res;
		}

		public static IEnumerable<T> ToEnumerable<T>(this T item)
		{
			yield return item;
		}

		public static T DeepClone<T>(this T obj)
		{
			using (var ms = new MemoryStream())
			{
				var formatter = new BinaryFormatter();
				formatter.Serialize(ms, obj);
				ms.Position = 0;

				return (T) formatter.Deserialize(ms);
			}
		}
	}
}