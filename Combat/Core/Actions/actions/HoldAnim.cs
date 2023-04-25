using Anjin.Actors;
using Combat.Data;
using Combat.Features.TurnOrder;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;

namespace Combat.Toolkit
{
	public class HoldAnim : BattleAnim
	{
		public HoldAnim([CanBeNull] Fighter fighter = null)
		{
			this.fighter = fighter;
		}

		private void GainHoldSP() { battle.AddSP(fighter, fighter.max_points.sp * 0.25f); }

		public override void RunInstant()
		{
			if (battle.EmitCancel(Signals.use_hold, fighter))
				return;

			GainHoldSP();
			RunInstant(new TurnQueryAnim(TurnOperations.Hold));
		}

		public override async UniTask RunAnimated()
		{
			if (fighter == null)
				return;

			if (battle.EmitCancel(Signals.use_hold, fighter))
				return;

			GainHoldSP();

			//if ((fighter != null) && (fighter.coach != null))
			//{
			//	fighter.coach.SetAction();
			//}

			await RunAnimated(new TurnQueryAnim(TurnOperations.Hold));
		}
	}
}