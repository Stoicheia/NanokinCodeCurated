using System;
using JetBrains.Annotations;

// ReSharper disable InconsistentNaming

namespace Anjin.Scripting
{
	/// <summary>
	/// Registers the target class as Userdata, for use in Lua scripts.
	/// This will both register the type as userdata & register the type itself as a global value.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
	[MeansImplicitUse]
	public class LuaUserdataAttribute : Attribute
	{
		/// <summary>
		/// Sets the name of the type in Lua.
		/// globals[TypeName] == typeof(this)
		/// </summary>
		[CanBeNull] public string TypeName;

		/// <summary>
		/// Sets the name of the userdata for static functions in Lua.
		/// </summary>
		[CanBeNull] public string StaticName;

		/// <summary>
		///  Creates a static userdata matching the type's name automatically.
		/// </summary>
		public bool StaticAuto;

		/// <summary>
		/// Include the descendants recursively. (userdata register only)
		/// </summary>
		public bool Descendants;

		public LuaUserdataAttribute([CanBeNull] string staticName = null, [CanBeNull] string typeName = null, bool staticAuto = false)
		{
			TypeName   = typeName;
			StaticAuto = staticAuto;
			StaticName = staticName;
		}
	}

	/// <summary>
	/// Marks a method for exposing into the Lua environment.
	/// DOES NOT SUPPORT OVERLOADING!!
	///
	/// Example:
	///
	/// [LuaGlobalFunc("my_awesome_function")]
	///	public static void MyAwesomeFunction(int foo, string bar) {
	///		...
	/// }
	///
	/// would be registered as a function in the global table, callable like so:
	///
	/// my_awesome_function(69, "420")
	/// </summary>
	[AttributeUsage(AttributeTargets.Method)]
	[MeansImplicitUse]
	public class LuaGlobalFuncAttribute : Attribute
	{
		[CanBeNull] public string Name;

		public LuaGlobalFuncAttribute([CanBeNull] string name = null)
		{
			Name = name;
		}
	}

	/// <summary>
	/// Registers the target enum as a lua type.
	/// </summary>
	[AttributeUsage(AttributeTargets.Enum)]
	public class LuaEnumAttribute : Attribute
	{
		/// <summary>
		/// Name of the table to access enum values.
		/// </summary>
		public string Name;

		/// <summary>
		/// Allows interpreting string values as enum values.
		/// </summary>
		public bool StringConvertible;

		public LuaEnumAttribute(string name = null, bool stringConvertible = false)
		{
			Name              = name;
			StringConvertible = stringConvertible;
		}
	}

	/// <summary>
	/// Sets the name for mapping to lua.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class LuaName : Attribute
	{
		public string Name;

		public LuaName(string name)
		{
			Name = name;
		}
	}

	public class LuaProxyTypesAttribute : Attribute
	{
		public Type[] Types;

		public LuaProxyTypesAttribute(params Type[] types)
		{
			Types = types;
		}

		public bool Descendants { get; set; }
	}

	public class LuaBoxAttribute : Attribute { }
}