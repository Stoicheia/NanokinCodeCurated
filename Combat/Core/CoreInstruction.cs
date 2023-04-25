using Combat.Data;
using Combat.Features.TurnOrder;
using Combat.Toolkit;
using JetBrains.Annotations;

namespace Combat
{
	/// <summary>
	/// A single instructions posted to the BattleCore.
	/// </summary>
	public struct CoreInstruction
	{
		public CoreOpcode op;

		// Data for any instruction
		// ----------------------------------------
		public float duration;

		// Trigger
		public Signals      signal;
		public TriggerEvent triggerEvent;
		public object       me;

		// Actions
		[CanBeNull] public BattleAnim   anim;
		[CanBeNull] public BattleAnim[] actions;

		public override string ToString()
		{
			return $"CoreInstruction: {op} {duration} {signal} {triggerEvent} {me} {anim} {actions}";
		}
	}
}