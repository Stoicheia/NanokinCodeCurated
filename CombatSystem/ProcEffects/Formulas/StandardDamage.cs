using Anjin.Scripting;
using Anjin.Util;
using Combat.Data;
using Combat.Entities;
using Data.Combat;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Combat.StandardResources
{
	/// <summary>
	/// Deal damage to a victim using a relative power value, factoring in...
	/// - Natural power of the damage (this is a relative number, the formulas are designed so 20 comes out to decent average damage. 10 would be weak damage, 30 is some real nice shit, and 40 I wouldn't wanna be on the receiving end of that, ...)
	/// - Power stat of the dealer
	/// - Combo multiplier
	/// - Critical hit chance and multiplier (zero by default/disabled)
	/// - Elemental Defense
	/// - Elemental Attack (if the proc has a dealer)
	/// This REQUIRES a dealer. using StandardDamage in a god proc is undefined behavior and impossible,
	/// as we need the power stat to correctly calculate the damage from the relative natural power value.
	/// For god procs with a flat damage that should still factor in defenses, use StandardHurt.
	/// </summary>
	[LuaUserdata]
	[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
	public class StandardDamage : ProcEffect
	{
		private const float DEFAULT_VARIANCE = 3.5f; // +-3.5% by default

		public float    power;
		public Elements element;
		public float    variance = DEFAULT_VARIANCE;

		public float criticalChance     = 0f; // Crits are not enabled by default by order of king kyle
		public float criticalMultiplier = 2f;

		public override Elements Element => element.Mod(Status.element);

		public override Natures Nature => element.Mod(Status.element).GetNature();

		public StandardDamage(Elements element, int power, float variance)
		{
			this.power    = power;
			this.element  = element;
			this.variance = variance >= 0 ? variance : DEFAULT_VARIANCE;
		}

		public override float HPChange
		{
			get
			{
				if (dealer == null)
				{
					Debug.LogError("Invalid use of StandardDamage without a dealer.");
					return -4201337; // This will make the bug far more clear and recognizable than not doing anything. ("x didn't deal any damage" VS "x dealt way too much damage?? wait wtf is that number oxy???")
				}

				// Base damage
				// ----------------------------------------
				float epow = Status.Power(power);

				float dmg = RNG.Chance(Status.CritLuck(criticalChance))
					? epow * Status.CritMult(criticalMultiplier)
					: epow;

				float vari = variance / 100f;

				dmg *= Mathf.Lerp(0.1f, 1f, (dealer.level ?? 0) / (float)StatConstants.MAX_LEVEL); // scale down to something more reasonable
				dmg *= battle.GetStats(dealer).power;
				dmg *= RNG.Range(1 - vari / 2f, 1 + vari / 2f);

				// Apply combo
				dmg *= battle.GetComboMultiplier(dealer);

				// Stat / Resistance multipliers
				float effatk = battle.GetOffense(dealer)[element];
				float effdef = battle.GetDefense(fighter)[element];
				dmg *= 1 + (effatk - effdef).Minimum(0); // Efficiency clash

				// Absorption

				// float resistanceMultiplier = element != Elements.none
				// ? battle.GetBracketEffects(fighter)[element]
				// : 1;

				return Status.HPChange(-dmg);
			}
		}

		public StandardDamage(Table tbl)
		{
			configure(tbl);
		}

		public void configure(Table tbl)
		{
			tbl.TryGet("power", out power, power);
			tbl.TryGet("element", out element, element);
			tbl.TryGet("elem", out element, element);
			tbl.TryGet("crit_chance", out criticalChance, criticalChance);
			tbl.TryGet("crit_multiplier", out criticalMultiplier, criticalMultiplier);
			// tbl.TryGet("bonus", out bonus, bonus);
			// tbl.TryGet("multiplier", out multiplier, multiplier);
		}

		protected override ProcEffectFlags ApplyFighter()
		{
			if (!fighter.status.engineFlags.Contains(EngineFlags.untargetable))
			{
				// Apply
				var chg = new PointChange
				{
					value    = new Pointf(HPChange),
					element  = element,
					critical = false
				};

				battle.AddPoints(fighter, chg);

				return ProcEffectFlags.VictimEffect;
			}
			else
			{
				return ProcEffectFlags.NoEffect;
			}
		}
	}
}