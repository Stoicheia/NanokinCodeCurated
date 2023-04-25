using System;
using Sirenix.OdinInspector;

namespace Util
{
	public class TypePickerAttribute : ShowInInspectorAttribute
	{
		public Type basetype;

		public TypePickerAttribute(Type basetype)
		{
			this.basetype = basetype;
		}
	}
}