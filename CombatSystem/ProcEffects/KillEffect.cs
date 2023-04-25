using Anjin.Scripting;
using MoonSharp.Interpreter;

namespace Combat.Data
{
	/// <summary>
	/// This applies to both fighters and slots
	/// </summary>
	public class KillEffect : ProcEffect
	{
		public Closure closure;
		public string  tag;
		public Table   tags;

		public KillEffect() { }

		public KillEffect(Fighter fighter)
		{
			this.fighter = fighter;
		}

		public KillEffect(Closure closure)
		{
			this.closure = closure;
		}

		public KillEffect(string tag)
		{
			this.tag = tag;
		}

		public KillEffect(Table tags)
		{
			this.tags = tags;
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			if (closure != null)
				battle.RemoveFighter(closure.Call().ToObject<Fighter>(), true);
			else if (tag != null)
			{
				foreach (Fighter fighter in battle.fighters)
					if (fighter.has_tag(tag))
						battle.RemoveFighter(fighter, true);

				foreach (State state in battle.states)
					if (state.has_tag(tag))
						battle.RemoveState(state);
			}
			else if (tags != null)
			{
				foreach (Fighter fighter in battle.fighters)
				foreach (TablePair tag in tags.Pairs)
				{
					if (fighter.has_tag(tag.Value.AsString()))
					{
						battle.RemoveFighter(fighter, true);
						break;
					}
				}
			}
			else
			{
				battle.RemoveFighter(fighter, true);
			}

			return ProcEffectFlags.VictimEffect;
		}

		protected override ProcEffectFlags ApplySlot()
		{
			fighter = slot.owner;
			return TryApplyFighter();
		}
	}
}