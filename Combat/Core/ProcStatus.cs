using System;
using Data.Combat;

namespace Combat.Data
{
	public class ProcStatus
	{
		public ModStat<float> hpchange,  spchange,  opchange,  ptchange;
		public ModStat<float> hpgain,    spgain,    opgain,    ptgain;
		public ModStat<float> hploss,    sploss,    oploss,    ptloss;
		public ModStat<float> hpdrain,   spdrain,   opdrain,   ptdrain;
		public ModStat<float> hppercent, sppercent, oppercent, ptpercent;

		public ModStat<float> power, potency,     percent;
		public ModStat<float> luck,  effect_luck, crit_luck;
		public ModStat<float> crit_mult;
		public ModStat<float> life;

		public Elements? element;

		public void set(ProcStat stat, float f)
		{
			GetMod(stat).set = f;
		}

		public void up(ProcStat stat, float f)
		{
			GetMod(stat).flat += f;
		}

		public void down(ProcStat stat, float f)
		{
			GetMod(stat).flat -= f;
		}

		public void scale(ProcStat stat, float f)
		{
			// These additions aren't typo, we do this because compound interest is evil (two 2x mods should result in 4x instead of 8x)
			if (f > 1)
				GetMod(stat).scale += f - 1;
			else
				GetMod(stat).scale -= 1 - f;
		}

		public void Reset()
		{
			hpgain      = ModStatExt.zero;
			hploss      = ModStatExt.zero;
			hpchange    = ModStatExt.zero;
			spgain      = ModStatExt.zero;
			sploss      = ModStatExt.zero;
			spchange    = ModStatExt.zero;
			opgain      = ModStatExt.zero;
			oploss      = ModStatExt.zero;
			opchange    = ModStatExt.zero;
			ptgain      = ModStatExt.zero;
			ptloss      = ModStatExt.zero;
			ptdrain     = ModStatExt.zero;
			hpdrain     = ModStatExt.zero;
			spdrain     = ModStatExt.zero;
			power       = ModStatExt.zero;
			potency     = ModStatExt.zero;
			luck        = ModStatExt.zero;
			effect_luck = ModStatExt.zero;
			crit_luck   = ModStatExt.zero;
			crit_mult   = ModStatExt.zero;
			life        = ModStatExt.zero;
			element     = null;
		}

		public void Add(ProcStatus other)
		{
			hpchange = ModStatExt.Add(hpchange, other.hpchange);
			spchange = ModStatExt.Add(spchange, other.spchange);
			opchange = ModStatExt.Add(opchange, other.opchange);
			ptchange = ModStatExt.Add(ptchange, other.ptchange);

			hpgain = ModStatExt.Add(hpgain, other.hpgain);
			spgain = ModStatExt.Add(spgain, other.spgain);
			opgain = ModStatExt.Add(opgain, other.opgain);
			ptgain = ModStatExt.Add(ptgain, other.ptgain);

			hploss = ModStatExt.Add(hploss, other.hploss);
			sploss = ModStatExt.Add(sploss, other.sploss);
			oploss = ModStatExt.Add(oploss, other.oploss);
			ptloss = ModStatExt.Add(ptloss, other.ptloss);

			hpdrain = ModStatExt.Add(hpdrain, other.hpdrain);
			spdrain = ModStatExt.Add(spdrain, other.spdrain);
			opdrain = ModStatExt.Add(opdrain, other.opdrain);
			ptdrain = ModStatExt.Add(ptdrain, other.ptdrain);

			power       = ModStatExt.Add(power, other.power);
			potency     = ModStatExt.Add(potency, other.potency);
			luck        = ModStatExt.Add(luck, other.luck);
			effect_luck = ModStatExt.Add(effect_luck, other.effect_luck);
			crit_luck   = ModStatExt.Add(crit_luck, other.crit_luck);
			crit_mult   = ModStatExt.Add(crit_mult, other.crit_mult);
			life        = ModStatExt.Add(life, other.life);

			element = other.element;
		}

		public ref ModStat<float> GetMod(ProcStat stat)
		{
			switch (stat)
			{
				case ProcStat.hpchange: return ref hpchange;
				case ProcStat.spchange: return ref spchange;
				case ProcStat.opchange: return ref opchange;
				case ProcStat.ptchange: return ref ptchange;

				case ProcStat.hpgain: return ref hpgain;
				case ProcStat.spgain: return ref spgain;
				case ProcStat.opgain: return ref opgain;
				case ProcStat.ptgain: return ref ptgain;

				case ProcStat.hploss: return ref hploss;
				case ProcStat.sploss: return ref sploss;
				case ProcStat.oploss: return ref oploss;
				case ProcStat.ptloss: return ref ptloss;

				case ProcStat.hpdrain: return ref hpdrain;
				case ProcStat.spdrain: return ref spdrain;
				case ProcStat.opdrain: return ref opdrain;
				case ProcStat.ptdrain: return ref ptdrain;

				case ProcStat.power:       return ref power;
				case ProcStat.potency:     return ref potency;
				case ProcStat.luck:        return ref luck;
				case ProcStat.effect_luck: return ref effect_luck;
				case ProcStat.crit_luck:   return ref crit_luck;
				case ProcStat.crit_mult:   return ref crit_mult;
				case ProcStat.life:        return ref life;

				case ProcStat.heal: return ref hpgain;
				case ProcStat.hurt: return ref hploss;

				default: throw new ArgumentOutOfRangeException(nameof(stat), stat, "");
			}
		}

		public float HPChange(float  v) => v.Mod0(side(v, ptgain, ptloss), side(v, hpgain, hploss), hpchange);
		public float SPChange(float  v) => v.Mod0(side(v, ptgain, ptloss), side(v, spgain, sploss), spchange);
		public float OPChange(float  v) => v.Mod0(side(v, ptgain, ptloss), side(v, opgain, oploss), opchange);
		public float HPPercent(float p) => p.Mod0(potency, ptpercent, hppercent, percent);
		public float SPPercent(float p) => p.Mod0(potency, ptpercent, sppercent, percent);
		public float OPPercent(float v) => v.Mod0(potency, ptpercent, oppercent, percent);
		public float HPDrain(float   v) => v.Mod0(potency, ptdrain, hpdrain);
		public float SPDrain(float   v) => v.Mod0(potency, ptdrain, spdrain);
		public float OPDrain(float   v) => v.Mod0(potency, ptdrain, opdrain);

		public float Power(float      basePower) => basePower.Mod0(potency, power);
		public float EffectLuck(float v)         => v.Mod0(luck, effect_luck);
		public float CritLuck(float   baseLuck)  => baseLuck.Mod0(luck, crit_luck);
		public float CritMult(float   baseMult)  => baseMult.Mod0(crit_mult);
		public int   Life(float       baseLife)  => (int)baseLife.Mod0(life);

		public static ModStat<T> side<T>(float v, ModStat<T> neg, ModStat<T> pos) where T : struct
		{
			return v < 0 ? neg : pos;
		}
	}
}