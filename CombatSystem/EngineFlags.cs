using System.Diagnostics.CodeAnalysis;
using Anjin.Scripting;
using JetBrains.Annotations;

namespace Combat.Data
{
	/// <summary>
	/// Assortment of flags which modify the behaviour of the battle engine at various points.
	/// </summary>
	[LuaEnum("eflag")]
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	[SuppressMessage("ReSharper", "InconsistentNaming")]
	public enum EngineFlags
	{
		none = 0,

		/// <summary>
		/// Cannot be interacted with.
		/// </summary>
		uninteractable,

		/// <summary>
		/// Cannot be interacted with physically.
		/// </summary>
		uninteractable_physical,

		/// <summary>
		/// Cannot be interacted with rangedly. (new word i invented)
		/// </summary>
		uninteractable_ranged,

		/// <summary>
		/// Indicates that the participant cannot be targeted.
		/// </summary>
		untargetable,

		/// <summary>
		/// Cannot change formation under any circumstance.
		/// </summary>
		unmovable,

		/// <summary>
		/// Indicates that the participant skips his action, doing nothing.
		/// e.g. a state that makes the victim skip their next action
		/// </summary>
		skip_turn,

		/// <summary>
		/// Indicates that the fighter cannot act. (for nanokins, that is skills and stickers)
		/// </summary>
		lock_act,

		/// <summary>
		/// Indicates that the fighter cannot move by themselves, as an action.
		/// </summary>
		lock_formation,

		/// <summary>
		/// Indicates that the fighter cannot use the flee action and leave the battle.
		/// </summary>
		cant_flee,

		/// <summary>
		/// Indicates the slot/fighter will be subjected to icy movement logic.
		/// </summary>
		icy,
	}
}