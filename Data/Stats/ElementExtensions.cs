namespace Data.Combat
{
	public static class ElementExtensions
	{
		public static bool IsPhysical(this Elements element)
		{
			switch (element)
			{
				case Elements.blunt:
				case Elements.pierce:
				case Elements.slash:
					return true;
			}

			return false;
		}

		public static bool IsMagical(this Elements element)
		{
			switch (element)
			{
				case Elements.gaia:
				case Elements.oida:
				case Elements.astra:
					return true;
			}

			return false;
		}
	}
}