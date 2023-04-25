using System.Collections.Generic;
using Combat.Data.VFXs;

namespace Combat.Data
{
	/// <summary>
	/// An object that can receive states
	/// </summary>
	public interface IStatee
	{
		Status      Status { get; }
		BattleActor Actor  { get; }
		void        AddVFX(VFX    vfx);
		void        RemoveVFX(VFX vfx);
	}
}