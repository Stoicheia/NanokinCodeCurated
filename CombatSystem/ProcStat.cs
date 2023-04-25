namespace Combat.Data
{
	public enum ProcStat
	{
		/// <summary>
		/// Modulate all HP changes.
		/// </summary>
		hpchange,

		/// <summary>
		/// Modulate all SP changes.
		/// </summary>
		spchange,

		/// <summary>
		/// Modulate all OP changes.
		/// </summary>
		opchange,

		/// <summary>
		/// Modulate all HP, SP and OP changes.
		/// </summary>
		ptchange,

		/// <summary>
		/// Modulate all HP gains.
		/// </summary>
		hpgain,

		/// <summary>
		/// Modulate all SP gains.
		/// </summary>
		spgain,

		/// <summary>
		/// Modulate all OP gains.
		/// </summary>
		opgain,

		/// <summary>
		/// Modulate all HP, SP and OP gains.
		/// </summary>
		ptgain,

		/// <summary>
		/// Modulate all HP losses.
		/// </summary>
		hploss,

		/// <summary>
		/// Modulate all SP losses.
		/// </summary>
		sploss,

		/// <summary>
		/// Modulate all OP losses.
		/// </summary>
		oploss,

		/// <summary>
		/// Modulate all HP, SP and OP losses.
		/// </summary>
		ptloss,

		/// <summary>
		/// Modulate HP drains.
		/// </summary>
		hpdrain,

		/// <summary>
		/// Modulate SP drains.
		/// </summary>
		spdrain,

		/// <summary>
		/// Modulate OP drains.
		/// </summary>
		opdrain,

		/// <summary>
		/// Modulate HP, SP and OP drains.
		/// </summary>
		ptdrain,

		/// <summary>
		/// Synonymous with hpgain.
		/// </summary>
		heal,

		/// <summary>
		/// Synonymous with hploss.
		/// </summary>
		hurt,

		/// <summary>
		/// Modulate power, heal, percentages, and drains.
		/// </summary>
		potency,

		/// <summary>
		/// Modulate abstract power values. (StandardDamage)
		/// </summary>
		power,

		/// <summary>
		/// Modulate all luck values.
		/// </summary>
		luck,

		/// <summary>
		/// Modulate random chances for chance effects. (ChanceEffect)
		/// </summary>
		effect_luck,

		/// <summary>
		/// Modulate the luck for critical hits.
		/// </summary>
		crit_luck,

		/// <summary>
		/// Modulate the critical multiplier.
		/// </summary>
		crit_mult,

		/// <summary>
		/// Modulate the life of states.
		/// </summary>
		life
	}
}