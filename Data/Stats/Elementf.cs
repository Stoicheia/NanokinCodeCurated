using System;
using System.Collections.Generic;
using Anjin.Scripting;
using UnityEngine;

namespace Data.Combat
{
	public enum ResistanceBracket { Frail, Weak, Norm, Resist, Immune, Absorb }

	[LuaUserdata]
	[Serializable]
	public struct Elementf
	{
		// Physical
		public float blunt;
		public float pierce;
		public float slash;

		// Magical
		public float gaia;
		public float astra;
		public float oida;

		// Natures
		public float physical;
		public float magical;

		public Elementf(float blunt    = 0,
			float             slash    = 0,
			float             pierce   = 0,
			float             gaia     = 0,
			float             astra    = 0,
			float             oida     = 0,
			float             physical = 0,
			float             magical  = 0
		)
		{
			this.blunt    = blunt;
			this.slash    = slash;
			this.pierce   = pierce;
			this.gaia     = gaia;
			this.astra    = astra;
			this.oida     = oida;
			this.physical = physical;
			this.magical  = magical;
		}

		public static Elementf Zero => new Elementf();
		public static Elementf One  => new Elementf(1, 1, 1, 1, 1, 1);

		public static Elementf operator -(Elementf v1) =>
			new Elementf
			{
				blunt    = -v1.blunt,
				pierce   = -v1.pierce,
				slash    = -v1.slash,
				oida     = -v1.oida,
				gaia     = -v1.gaia,
				astra    = -v1.astra,
				physical = -v1.physical,
				magical  = -v1.magical,
			};

		public static Elementf operator +(Elementf v1, float v2) =>
			new Elementf
			{
				blunt    = v1.blunt + v2,
				slash    = v1.slash + v2,
				pierce   = v1.pierce + v2,
				oida     = v1.oida + v2,
				gaia     = v1.gaia + v2,
				astra    = v1.astra + v2,
				physical = v1.physical + v2,
				magical  = v1.magical + v2,
			};

		public static Elementf operator -(Elementf v1, float v2) =>
			new Elementf
			{
				blunt    = v1.blunt - v2,
				slash    = v1.slash - v2,
				pierce   = v1.pierce - v2,
				oida     = v1.oida - v2,
				gaia     = v1.gaia - v2,
				astra    = v1.astra - v2,
				physical = v1.physical - v2,
				magical  = v1.magical - v2,
			};

		public static implicit operator Elementf(float v2) =>
			new Elementf
			{
				blunt    = v2,
				slash    = v2,
				pierce   = v2,
				oida     = v2,
				gaia     = v2,
				astra    = v2,
				physical = v2,
				magical  = v2,
			};


		public static Elementf operator +(Elementf v1, Elementf v2) =>
			new Elementf
			{
				blunt    = v1.blunt + v2.blunt,
				slash    = v1.slash + v2.slash,
				pierce   = v1.pierce + v2.pierce,
				oida     = v1.oida + v2.oida,
				gaia     = v1.gaia + v2.gaia,
				astra    = v1.astra + v2.astra,
				physical = v1.physical + v2.physical,
				magical  = v1.magical + v2.magical,
			};

		public static Elementf operator *(Elementf v1, Elementf v2) =>
			new Elementf
			{
				blunt    = v1.blunt * v2.blunt,
				slash    = v1.slash * v2.slash,
				pierce   = v1.pierce * v2.pierce,
				oida     = v1.oida * v2.oida,
				gaia     = v1.gaia * v2.gaia,
				astra    = v1.astra * v2.astra,
				physical = v1.physical * v2.physical,
				magical  = v1.magical * v2.magical
			};

		public static Elementf operator *(Elementf v1, float mul) =>
			new Elementf
			{
				blunt    = v1.blunt * mul,
				pierce   = v1.pierce * mul,
				slash    = v1.slash * mul,
				oida     = v1.oida * mul,
				gaia     = v1.gaia * mul,
				astra    = v1.astra * mul,
				physical = v1.physical * mul,
				magical  = v1.magical * mul,
			};

		public Elementf Floored()
		{
			blunt    = Mathf.FloorToInt(blunt);
			slash    = Mathf.FloorToInt(slash);
			pierce   = Mathf.FloorToInt(pierce);
			oida     = Mathf.FloorToInt(oida);
			gaia     = Mathf.FloorToInt(gaia);
			astra    = Mathf.FloorToInt(astra);
			physical = Mathf.FloorToInt(physical);
			magical  = Mathf.FloorToInt(magical);

			return this;
		}

		public override string ToString() => $"({blunt}/{slash}/{pierce} | {gaia}/{astra}/{oida} | {physical}/{magical})";

		public float this[Elements element]
		{
			get
			{
				switch (element)
				{
					case Elements.none:   return 0;
					case Elements.blunt:  return blunt;
					case Elements.slash:  return slash;
					case Elements.pierce: return pierce;
					//
					case Elements.gaia:  return gaia;
					case Elements.oida:  return oida;
					case Elements.astra: return astra;
					default:
						throw new ArgumentOutOfRangeException(nameof(element), element, null);
				}
			}
			set
			{
				switch (element)
				{
					case Elements.blunt:
						blunt = value;
						break;
					case Elements.slash:
						slash = value;
						break;
					case Elements.pierce:
						pierce = value;
						break;
					//
					case Elements.gaia:
						gaia = value;
						break;
					case Elements.oida:
						oida = value;
						break;
					case Elements.astra:
						astra = value;
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(element), element, null);
				}
			}
		}

		public float this[Natures nature]
		{
			get
			{
				switch (nature)
				{
					case Natures.None:     return 0;
					case Natures.Physical: return physical;
					case Natures.Magical:  return magical;
					default:
						throw new ArgumentOutOfRangeException(nameof(nature), nature, null);
				}
			}
		}

		public Elementf Min(int max)
		{
			return new Elementf
			{
				blunt    = Mathf.Max(max, blunt),
				pierce   = Mathf.Max(max, pierce),
				slash    = Mathf.Max(max, slash),
				oida     = Mathf.Max(max, oida),
				gaia     = Mathf.Max(max, gaia),
				astra    = Mathf.Max(max, astra),
				physical = Mathf.Max(max, physical),
				magical  = Mathf.Max(max, magical),
			};
		}


		public static ResistanceBracket GetBracket(float value)
		{
			// Same code as above but with if-else
			if (value < -50) return ResistanceBracket.Frail;
			else if (value < 0) return ResistanceBracket.Weak;
			else if (value < 50) return ResistanceBracket.Norm;
			else if (value < 100) return ResistanceBracket.Resist;
			else if ((int) value == 100) return ResistanceBracket.Immune;
			else return ResistanceBracket.Absorb;
		}

		public static float GetBracketEffect(ResistanceBracket bracket)
		{
			switch (bracket)
			{
				case ResistanceBracket.Frail:  return 2;
				case ResistanceBracket.Weak:   return 1.5f;
				case ResistanceBracket.Norm:   return 1;
				case ResistanceBracket.Resist: return 0.5f;
				case ResistanceBracket.Immune: return 0;
				default:                       return -1;
			}
		}

		public Elementf GetBracketEffects()
		{
			return new Elementf
			{
				blunt  = GetBracketEffect(GetBracket(blunt)),
				pierce = GetBracketEffect(GetBracket(pierce)),
				slash  = GetBracketEffect(GetBracket(slash)),
				oida   = GetBracketEffect(GetBracket(oida)),
				gaia   = GetBracketEffect(GetBracket(gaia)),
				astra  = GetBracketEffect(GetBracket(astra))
			};
		}

		public Elementf Clamp(float min, float max)
		{
			return new Elementf
			{
				blunt    = Mathf.Clamp(blunt, min, max),
				pierce   = Mathf.Clamp(pierce, min, max),
				slash    = Mathf.Clamp(slash, min, max),
				oida     = Mathf.Clamp(oida, min, max),
				gaia     = Mathf.Clamp(gaia, min, max),
				astra    = Mathf.Clamp(astra, min, max),
				physical = Mathf.Clamp(physical, min, max),
				magical  = Mathf.Clamp(magical, min, max),
			};
		}
	}
}