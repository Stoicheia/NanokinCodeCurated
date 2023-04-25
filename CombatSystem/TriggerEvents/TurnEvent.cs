using Combat.Data;
using Combat.Features.TurnOrder;
using Combat.Features.TurnOrder.Events;
using JetBrains.Annotations;

[UsedImplicitly]
public class RoundEvent : TriggerEvent
{
	public readonly int round_id;

	public RoundEvent(int id)
	{
		round_id = id;
	}
}

[UsedImplicitly]
public class TurnEvent : TriggerEvent
{
	public readonly Action action;

	public int round_counter = 0;

	public bool round_first => round_counter == 0;

	public TurnEvent(ITurnActer ev, Action action)
	{
		me        = ev;
		this.action = action;
	}
}

// TODO split off the effect into a new ProcEffectEvent