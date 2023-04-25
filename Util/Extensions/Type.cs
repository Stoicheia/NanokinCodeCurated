using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Vexe.Runtime.Extensions;

namespace Util.Extensions
{
	public static partial class Extensions
	{
		private static readonly Dictionary<Type, string> Aliases =
			new Dictionary<Type, string>()
			{
				{typeof(byte), "byte"},
				{typeof(sbyte), "sbyte"},
				{typeof(short), "short"},
				{typeof(ushort), "ushort"},
				{typeof(int), "int"},
				{typeof(uint), "uint"},
				{typeof(long), "long"},
				{typeof(ulong), "ulong"},
				{typeof(float), "float"},
				{typeof(double), "double"},
				{typeof(decimal), "decimal"},
				{typeof(object), "object"},
				{typeof(bool), "bool"},
				{typeof(char), "char"},
				{typeof(string), "string"},
				{typeof(void), "void"}
			};

		public static void FindType(this Type type, string name) { }


		/// <summary>
		///     Gets all the children types (abstract/concrete) of this type from the specified assembly
		///     Pass true to directlyUnder to only get the children that are directly under this type (i.e. no grandchildren)
		/// </summary>
		public static IEnumerable<Type> GetChildren(this Type type, Assembly from, bool directlyUnder = false)
		{
			return from.GetTypes().Where(t => t.IsA(type) && (!directlyUnder || t.BaseType == type)).Disinclude(type);
		}

		/// <summary>
		///     Gets all the children types (abstract/concrete) of this type from the specified assembly
		///     Pass true to directlyUnder to only get the children that are directly under this type (i.e. no grandchildren)
		/// </summary>
		public static IEnumerable<Type> GetChildren(this Type type, bool directlyUnder = false)
		{
			return GetChildren(type, type.Assembly, directlyUnder);
		}

		/// <summary>
		///     Gets all the concrete (non-abstract) children of this type from the specified assembly
		///     Pass true to directlyUnder to only get the children that are directly under this type (i.e. no grandchildren)
		/// </summary>
		public static IEnumerable<Type> GetConcreteChildren(this Type type, Assembly from, bool directlyUnder = false)
		{
			return GetChildren(type, from, directlyUnder).Where(c => !c.IsAbstract);
		}

		public static IEnumerable<Type> GetConcreateChildren(this Type type, Assembly assembly)
		{
			return GetConcreteChildren(type, assembly, false);
		}

		/// <summary>
		///     Gets all the concrete (non-abstract) children of this type from its own assembly
		///     Pass true to directlyUnder to only get the children that are directly under this type (i.e. no grandchildren)
		/// </summary>
		public static IEnumerable<Type> GetConcreteChildren(this Type type, bool directlyUnder = false)
		{
			return GetConcreteChildren(type, type.Assembly, directlyUnder);
		}

		public static Type[] GetConcreteChildren(this Type type, string[] dlls)
		{
			List<Type> allTypes = new List<Type>();
			foreach (string dll in dlls)
			{
				IEnumerable<Type> types = type.GetConcreteChildren(Assembly.LoadFile(dll));
				foreach (Type t in types) allTypes.Add(t);
			}

			return allTypes.ToArray();
		}

		private static void GetAllDerivedTypesRecursively(this Type[] types, Type type1, ref List<Type> results)
		{
			if (type1.IsGenericType)
			{
				GetDerivedFromGeneric(types, type1, ref results);
			}
			else
			{
				GetDerivedFromNonGeneric(types, type1, ref results);
			}
		}

		public static void GetDerivedFromGeneric(this Type[] types, Type type, ref List<Type> results)
		{
			var derivedTypes = types
				.Where(t => t.BaseType != null && t.BaseType.IsGenericType &&
				            t.BaseType.GetGenericTypeDefinition() == type).ToList();
			results.AddRange(derivedTypes);
			foreach (Type derivedType in derivedTypes)
			{
				GetAllDerivedTypesRecursively(types, derivedType, ref results);
			}
		}


		public static void GetDerivedFromNonGeneric(this Type[] types, Type type, ref List<Type> results)
		{
			var derivedTypes = types.Where(t => t != type && type.IsAssignableFrom(t)).ToList();

			results.AddRange(derivedTypes);
			foreach (Type derivedType in derivedTypes)
			{
				GetAllDerivedTypesRecursively(types, derivedType, ref results);
			}
		}
	}
}