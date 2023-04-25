using System;
using Sirenix.OdinInspector;

namespace Util.Odin.Attributes
{
	/// <summary>
	/// A combination of ShowInInspector and HideInEditorMode.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Method)]
	public class ShowInPlayAttribute : ShowInInspectorAttribute
	{
	}
}