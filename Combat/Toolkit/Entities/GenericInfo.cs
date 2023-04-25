using System.Collections.Generic;
using Data.Combat;
using Data.Shops;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Util.Addressable;

namespace Combat.Entities
{
	public class GenericInfo : FighterInfo
	{
		//public string                            ActorPrefabAddress;
		public ComponentRef<GenericFighterActor> ActorPrefab;

		public          string DisplayName;
		public override string Name => DisplayName;


		public Pointf   MaxPoints       = new Pointf(1, 1, 1);
		public Statf    MaxStats        = Statf.Zero;
		public Elementf MaxEfficiencies = Elementf.Zero;

		public override Pointf   StartPoints  => MaxPoints;
		public override Pointf   Points       => StartPoints;
		public override Statf    Stats        => MaxStats;
		public override Elementf Resistances => MaxEfficiencies;
		public override int      Priority => (int)Stats.speed;
		public override int      Actions    => (int)Stats.ap - 1;

		public List<SkillAsset> Skills;

		public Dictionary<string, string> StringData;
		public Dictionary<string, int>    IntData;

		public                                 bool         UseCustomLootValues;
		[ShowIf("UseCustomLootValues")] public int          xpLoot;
		[ShowIf("UseCustomLootValues")] public int          rpLoot;
		[ShowIf("UseCustomLootValues")] public LootDropInfo itemLoot;
		public override                        int          XPLoot => UseCustomLootValues ? xpLoot : 0;
		public override                        int          RPLoot => UseCustomLootValues ? rpLoot : 0;

		public override LootDropInfo ItemLoot => UseCustomLootValues ? itemLoot : LootDropInfo.Empty();

		public GenericInfo()
		{
			Skills = new List<SkillAsset>();

			StringData = new Dictionary<string, string>();
			StringData.Clear();

			IntData = new Dictionary<string, int>();
			IntData.Clear();
		}

		public override void GetSkills(List<SkillInfo> result)
		{
			foreach (SkillAsset asset in Skills)
			{
				result.Add(new SkillInfo { limb = null, skill = asset });
			}
		}

		public override bool has_int(string key)
		{
			return IntData.ContainsKey(key);
		}

		public override int load_int(string key)
		{
			if (!has_int(key))
			{
				IntData.Add(key, 0);
			}

			return IntData[key];
		}

		public override void save_int(string key, int value)
		{
			if (has_int(key))
			{
				IntData[key] = value;
			}
			else
			{
				IntData.Add(key, value);
			}
		}

		public override void delete_int(string key)
		{
			if (has_int(key))
			{
				IntData.Remove(key);
			}
		}

		public override void clear_ints()
		{
			IntData.Clear();
		}

		public override bool has_string(string key)
		{
			return StringData.ContainsKey(key);
		}

		public override string load_string(string key)
		{
			if (!has_string(key))
			{
				StringData.Add(key, "");
			}

			return StringData[key];
		}

		public override void save_string(string key, string value)
		{
			if (has_string(key))
			{
				StringData[key] = value;
			}
			else
			{
				StringData.Add(key, value);
			}
		}

		public override void delete_string(string key)
		{
			if (has_string(key))
			{
				StringData.Remove(key);
			}
		}

		public override void clear_strings()
		{
			StringData.Clear();
		}
	}
}