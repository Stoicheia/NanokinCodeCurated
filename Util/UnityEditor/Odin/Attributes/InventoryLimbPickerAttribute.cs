using System;
using Data.Nanokin;

namespace Util.Odin.Attributes
{
	public class InventoryLimbPickerAttribute : Attribute
	{
		public LimbType LimbType { get; }

		public InventoryLimbPickerAttribute(LimbType limbType)
		{
			LimbType = limbType;
		}
	}
}