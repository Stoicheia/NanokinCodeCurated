using System;
using Sirenix.OdinInspector;

namespace Util.Odin.Attributes
{
	public class DebugVarAttribute : ShowInPlayAttribute
	{
	}

	public class DebugVarsAttribute : Attribute
	{

	}

	/// <summary>
	///
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
	public class ShowInDebug : ShowInInspectorAttribute
	{

	}
}