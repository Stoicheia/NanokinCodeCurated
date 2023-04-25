using Anjin.Util;
using Data.Combat;
using JetBrains.Annotations;

namespace Combat.Data
{
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class HurtHP : ProcEffect
	{
		/// <summary>
		/// Gets the damage element.
		/// </summary>
		public Elements element;

		/// <summary>
		/// Gets the actual points to deduct off of the victim's hp.
		/// </summary>
		public int value;

		/// <summary>
		/// Should we present it as a critical hit visually?
		/// </summary>
		public bool showCritical;

		//public override float    HPChange => stats.HPChange(-((value + bonus) * multiplier));

		public override float HPChange => Status.HPChange(-value);

		public override Elements Element  => element;
		public override Natures  Nature   => element.GetNature();

		public HurtHP(float value, Elements element, bool showCritical = false) : this(value.Floor(), element, showCritical) { }

		public HurtHP(int value, Elements element, bool showCritical = false)
		{
			this.value        = value;
			this.element      = element;
			this.showCritical = showCritical;
		}

		/// <summary>
		/// Gets a value indicating whether or not if "miss" should be shown. (we don't want to show zero-damage values)
		/// </summary>
		public bool IsMiss => value <= 0;

		protected override ProcEffectFlags ApplyFighter()
		{
			battle.AddPoints(fighter, new PointChange
			{
				value    = new Pointf(HPChange),
				element  = element.Mod(Status.element),
				critical = showCritical
			});

			//else
			//{
			//	Pointf points = victim.currentPoints;
			//	Pointf max = battle.GetMaxPoints(victim);
			//	var amount = chg.value;

			//	amount.hp = -1;
			//	amount.op = 0;
			//	amount.sp = 0;

			//	points += amount;
			//	points = points.Max(max);
			//	points = points.Floored(); // Remove floating points

			//	chg = amount;

			//	victim.currentPoints = points;
			//	battle.ShowPointChange(victim, chg);
			//	victim.coach?.OnPointsChanged();
			//}

			return ProcEffectFlags.VictimEffect;
		}
	}
}