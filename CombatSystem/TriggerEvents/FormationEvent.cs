using Combat;
using Combat.Data;
using JetBrains.Annotations;
using UnityEngine;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class FormationEvent : TriggerEvent
{
	public Battle.SlotSwap swap;

	[NotNull]
	public Slot slot => swap.me.slot;

	public Vector2Int coord => swap.me.slot.coord;

	[CanBeNull]
	public object them => swap.swapee?.fighter;

	public Slot from => swap.@from;
	public Slot to => swap.me.slot;
}