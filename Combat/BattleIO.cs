using System.Collections.Generic;
using Anjin.Nanokin.Park;
using Data.Shops;
using JetBrains.Annotations;
using UnityEngine;
using Util.Odin.Attributes;

namespace Combat.Startup
{
	/// <summary>
	/// Information that the core can read from and write to.
	///
	/// * The input info is mostly used for initializing the fight.
	/// * The output info is mostly used from outside after the core has terminated.
	/// </summary>
	public class BattleIO
	{
		// INPUT
		// ----------------------------------------

		/// <summary>
		/// Recipe to initialize the fight with.
		/// </summary>
		public BattleRecipe recipe;

		/// <summary>
		/// Arena that the battle takes place in.
		/// Required for an animated battle.
		/// </summary>
		public Arena arena = null;

		/// <summary>
		/// Overrides the arena's music
		/// NOTE: Temporary
		/// </summary>
		public AudioClip music = null;

		/// <summary>
		/// Player advantage, only used for animations.
		/// </summary>
		public EncounterAdvantages advantage = EncounterAdvantages.Neutral;

		/// <summary>
		/// Total XP awarded to a player that wins this fight.
		/// Automatically populated by the battle recipe, generally.
		/// </summary>
		public int xpLoots = 0;

		/// <summary>
		/// Total RP awarded to a player that wins this fight.
		/// Automatically populated by the battle recipe, generally.
		/// </summary>
		public int rpLoots = 0;

		/// <summary>
		/// Whether or not the battle can be restarted internally by a retry implementation e.g. player menus.
		/// </summary>
		public bool canRetry = true;

		/// <summary>
		/// Whether or not the battle can be stopped by a flee implementation e.g. player menus.
		/// </summary>
		public bool canFlee = true;

		/// <summary>
		/// Other loot awarded to the player.
		/// </summary>
		// Note for future programmers: Don't try to consolidate all loot received into one LootEntry variable. This is okay.
		public LootDropInfo itemLoots;



		// OUTPUT
		// ----------------------------------------

		/// <summary>
		/// Final outcome of the fight. (relative to a player)
		/// </summary>
		public BattleOutcome outcome;
	}
}