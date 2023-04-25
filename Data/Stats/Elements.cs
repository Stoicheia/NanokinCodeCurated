using System.Collections.Generic;
using API.PropertySheet.Elements;

namespace Data.Combat
{
	public static class ElementsExtensions
	{
		public static Natures GetNature(this Elements elem)
		{
			switch (elem)
			{
				case Elements.blunt:
				case Elements.pierce:
				case Elements.slash:
					return Natures.Physical;

				// Magical
				case Elements.gaia:
				case Elements.oida:
				case Elements.astra:
					return Natures.Magical;

				default:
					return Natures.None;
			}
		}
	}

	public static class ElementsUtil
	{
		public static Elements String2Element(string s)
		{
			if (s == "blunt") return Elements.blunt;
			if (s == "pierce") return Elements.pierce;
			if (s == "slash") return Elements.slash;
			if (s == "gaia") return Elements.gaia;
			if (s == "oida") return Elements.oida;
			if (s == "astra") return Elements.astra;
			return Elements.none;
		}

		public static HashSet<Elements> String2Elements(string s)
		{
			if (s == "physical") return new HashSet<Elements>() {Elements.blunt, Elements.pierce, Elements.slash };
			if (s == "magical") return new HashSet<Elements>() {Elements.gaia, Elements.oida, Elements.astra };
			return new HashSet<Elements>() {String2Element(s)};
		}

		public static HashSet<Elements> AllElements()
		{
			return new HashSet<Elements>()
			{
				Elements.none,
				Elements.blunt,
				Elements.pierce,
				Elements.slash,
				Elements.gaia,
				Elements.oida,
				Elements.astra
			};
		}
	}

	public enum Elements
	{
		none,

		// Physical
		blunt,
		pierce,
		slash,

		// Magical
		gaia,
		oida,
		astra
	}
}