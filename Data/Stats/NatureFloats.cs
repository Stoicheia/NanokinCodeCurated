using System;
using Anjin.Scripting;

namespace Data.Combat
{
	// [LuaUserdata]
	// [Serializable]
	// public struct NatureFloats
	// {
	// 	public float physical;
	// 	public float magical;
	//
	// 	public NatureFloats(float physical = 0, float magical = 0)
	// 	{
	// 		this.physical = physical;
	// 		this.magical  = magical;
	// 	}
	//
	// 	public static NatureFloats Zero => new NatureFloats();
	// 	public static NatureFloats One  => new NatureFloats(1, 1);
	//
	// 	public static NatureFloats operator +(NatureFloats v1, NatureFloats v2)
	// 	{
	// 		return new NatureFloats(
	// 			v1.physical + v2.physical,
	// 			v1.magical + v2.magical);
	// 	}
	//
	// 	public static NatureFloats operator *(NatureFloats v1, NatureFloats v2)
	// 	{
	// 		return new NatureFloats(
	// 			v1.physical * v2.physical,
	// 			v1.magical * v2.magical);
	// 	}
	//
	// 	public static NatureFloats operator -(NatureFloats v1)
	// 	{
	// 		return new NatureFloats(
	// 			-v1.physical,
	// 			-v1.magical);
	// 	}
	//
	//
	// 	public float this[Elements elem] => this[elem.GetNature()];
	//
	// 	public float this[ElementNatures nature]
	// 	{
	// 		get
	// 		{
	// 			switch (nature)
	// 			{
	// 				case ElementNatures.None:     return 1;
	// 				case ElementNatures.Physical: return physical;
	// 				case ElementNatures.Magical:  return magical;
	// 				default:
	// 					throw new ArgumentOutOfRangeException(nameof(nature), nature, null);
	// 			}
	// 		}
	// 	}
	// }
}