using Anjin.Scripting;
using Anjin.Util;
using Combat.Data;
using Data.Combat;
using JetBrains.Annotations;
using MoonSharp.Interpreter.Interop;

namespace Combat.StandardResources
{
	/// <summary>
	/// Deal damage to a victim, factoring in its defense. If the proc has a dealer, its attack is also factored in.
	/// - Elemental Defense
	/// - Elemental Attack (if the proc has a dealer)
	/// </summary>
	[LuaUserdata]
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class StandardHurt : ProcEffect
	{
		public const float RNG_RANGE = 0.085f;

		public float    damage;
		public Elements element;

		public float criticalChance     = 0f; // Crits are not enabled by default by order of king kyle
		public float criticalMultiplier = 2f;

		public override Elements Element => element.Mod(Status.element);

		[MoonSharpVisible(true)]
		public StandardHurt(Elements element, int damage)
		{
			this.damage  = damage;
			this.element = element;
		}

		public override float HPChange { get; }
		// public override void XAlter([NotNull] Table table, Trigger.XAlters xalter)
		// {
		// 	base.XAlter(table, xalter);
		//
		// 	XAlterFloat(ref damage, xalter, table.Get("damage"));
		// }

		protected override ProcEffectFlags ApplyFighter()
		{
			float dmg = damage;

			// Stat modifications
			float atk = dealer != null ? battle.GetOffense(dealer)[element] : 0;
			float def = battle.GetDefense(fighter)[element];

			// float resistanceMultiplier = element != Elements.none ? battle.GetBracketEffects(fighter)[element] : 1;

			dmg *= 1 + (atk - def).Minimum(0);
			dmg =  Status.HPChange(dmg);

			// Final result!
			battle.AddPoints(fighter, new PointChange
			{
				value   = new Pointf(-dmg),
				element = element
			});

			return ProcEffectFlags.VictimEffect;
		}
	}
}