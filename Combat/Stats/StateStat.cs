namespace Combat.Data
{
	/// <summary>
	/// Wide list of 'stats' in combat that can be
	/// modified.
	/// </summary>
	public enum StateStat
	{
		// Level of the fighter
		lvl,

		// Points
		points,
		hp,
		sp,
		op,

		hp_gain,
		sp_gain,
		op_gain,


		// Stats
		stats,
		power,
		speed,
		will,
		ap,

		// Resistance
		res,
		res_blunt,
		res_slash,
		res_pierce,
		res_gaia,
		res_oida,
		res_astra,
		res_physical,
		res_magical,

		// Offense (efficiency for attacking side only)
		atk,
		atk_blunt,
		atk_slash,
		atk_pierce,
		atk_gaia,
		atk_oida,
		atk_astra,
		atk_physical,
		atk_magical,

		// Defense (efficiency for defending size only)
		def,
		def_blunt,
		def_slash,
		def_pierce,
		def_gaia,
		def_oida,
		def_astra,
		def_physical,
		def_magical,

		// Brain which controls the fighter
		brain,

		// Use properties
		// use_target,
		// skill_target,
		// sticker_target,
		use_target_options,
		skill_target_options,
		sticker_target_options,
		use_target_picks,
		skill_target_picks,
		sticker_target_picks,

		use_cost,
		skill_cost,
		sticker_cost,

		skill_usable,
		sticker_usable,

		// etc.
		eflag,

		// State properties
		state_tag,

		// Transient values
		hurt,
		heal,
		hurt_hp,
		hurt_sp,
		hurt_op,
		heal_hp,
		heal_sp,
		heal_op,
	}

	public static class StateStatExtensions
	{
		public static bool IsTargetOptions(this StateStat stat)
		{
			switch (stat)
			{
				case StateStat.skill_target_options:
				case StateStat.sticker_target_options:
				case StateStat.use_target_options:
					return true;
			}

			return false;
		}

		public static bool IsTargetPicks(this StateStat stat)
		{
			switch (stat)
			{
				case StateStat.skill_target_picks:
				case StateStat.sticker_target_picks:
				case StateStat.use_target_picks:
					return true;
			}

			return false;
		}

		public static bool IsUseCost(this StateStat stat)
		{
			int v = (int)stat;
			return v >= (int)StateStat.use_cost && stat <= StateStat.sticker_cost;
		}
	}
}