using System.Collections.Generic;
using Anjin.Scripting;
using Assets.Nanokins;
using Data.Combat;
using Data.Shops;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Puppets.Assets;
using SaveFiles;
using SaveFiles.Elements.Inventory.Items;
using UnityEngine;

namespace Combat
{
	/// <summary>
	/// The static base info and stats for a fighter.
	/// </summary>
	[LuaUserdata]
	public abstract class FighterInfo
	{
		// Base properties of the fighter
		// ----------------------------------------

		[NotNull]
		public virtual string Name => "Nameless";

		[NotNull]
		public virtual string FormationMethod => "neighbors";

		public virtual int              Level       => 1;
		public virtual Pointf           Points      => Pointf.Zero;
		public virtual Pointf           StartPoints => Points;
		public virtual Statf            Stats       => Statf.Zero;
		public virtual Elementf         Resistances => Elementf.Zero;
		public virtual int              Actions     => 0;
		public virtual int              Priority    => 0;
		public         FighterBaseState BaseState   => new FighterBaseState();

		public virtual void GetSkills(List<SkillInfo> result) { }

		[NotNull]   public virtual string             DNA => "";
		[CanBeNull] public         string             Sample = "";
		[CanBeNull] public virtual SkillGroup[]       SkillGroups      => null;
		[CanBeNull] public virtual List<StickerAsset> Stickers         => null;
		[CanBeNull] public virtual string             DefaultAI        => null;
		[CanBeNull] public virtual GameObject         SlotShadowPrefab => null;

		public virtual int XPLoot => 0;

		public virtual int RPLoot => 0;

		public virtual LootDropInfo ItemLoot => LootDropInfo.Empty();

		/// <summary>
		/// Copy the properties of the fighter back to this info.
		/// </summary>
		public virtual void SaveStats(Fighter fter) { }

		[UsedImplicitly]
		public virtual bool has_int(string key) => false;

		[UsedImplicitly]
		public virtual int load_int(string key) => 0;

		[UsedImplicitly]
		public virtual void save_int(string key, int value) { }

		[UsedImplicitly]
		public virtual void delete_int(string key) { }

		[UsedImplicitly]
		public virtual void clear_ints() { }

		[UsedImplicitly]
		public virtual bool has_string(string key) => false;

		[UsedImplicitly, NotNull]
		public virtual string load_string(string key) => "";

		[UsedImplicitly]
		public virtual void save_string(string key, string value) { }

		[UsedImplicitly]
		public virtual void delete_string(string key) { }

		[UsedImplicitly]
		public virtual void clear_strings() { }

		public struct SkillGroup
		{
			public string           name;
			public List<SkillAsset> skills;

			public SkillGroup(string name, List<SkillAsset> skills)
			{
				this.name   = name;
				this.skills = skills;
			}
		}

		public virtual void ConfigureTB([CanBeNull] Table tb) { }
	}

	public struct SkillInfo
	{
		public SkillAsset skill;
		[CanBeNull]
		public ScriptableLimb limb;
	}
}