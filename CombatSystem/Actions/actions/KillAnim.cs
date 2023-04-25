using Data.Combat;
using JetBrains.Annotations;

namespace Combat.Toolkit
{
	public class KillAnim : BattleAnim
	{
		public KillAnim([CanBeNull] Fighter fighter = null)
		{
			this.fighter = fighter;
		}

		public override void RunInstant()
		{
			Pointf current = fighter.points;
			current.hp = 0;
			battle.SetPoints(fighter, current);
		}
	}
}