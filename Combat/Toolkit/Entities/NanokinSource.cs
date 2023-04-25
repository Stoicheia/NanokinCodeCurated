using System.Collections.Generic;
using Anjin.Scripting;
using Data.Combat;
using Data.Nanokin;
using Data.Shops;
using UnityEngine;

namespace Combat.Entities
{
	/// <summary>
	/// A nanokin entity in a battle.
	/// </summary>
	[LuaUserdata]
	public class NanokinInfo : FighterInfo
	{
		public bool            unlockSkills;
		public NanokinInstance instance;

		public NanokinInfo(NanokinInstance data)
		{
			instance = data;
			data.RecalculateStats();
		}

		public override string   Name            => instance.Name;
		public override int      Level           => instance.Level;
		public override Statf    Stats           => instance.Stats;
		public override Elementf Resistances    => instance.Efficiencies /* * (1 / 100f)*/;
		public override Pointf   StartPoints     => new Pointf(instance.Points.hp * instance.MaxPoints.hp, instance.Points.sp * instance.MaxPoints.sp, instance.Points.op);
		public override Pointf   Points          => instance.MaxPoints;
		public override string   FormationMethod => instance.Body.Asset.MoveTargeter;

		public override void GetSkills(List<SkillInfo> result)
		{
			foreach (LimbInstance limb in instance.Limbs)
			{
				List<SkillAsset> unlockedSkills = limb.FindUnlockedSkills();
				foreach (SkillAsset skill in unlockedSkills)
				{
					result.Add(new SkillInfo
					{
						skill = skill,
						limb  = limb.Asset
					});
				}
			}
		}

		public override int XPLoot => instance.XPLoot;
		public override int RPLoot => instance.RPLoot;
		public override LootDropInfo ItemLoot => instance.ItemLoot;

		public override int        Priority     => (int)Stats.speed;
		public override int        Actions        => (int)instance.Stats.ap - 1;
		public override GameObject SlotShadowPrefab => null;
		public override string     DefaultAI        => instance.ai ?? (instance.NanokinAsset != null ? instance.NanokinAsset.DefaultAI : null);

		public override SkillGroup[] SkillGroups => new[]
		{
			new SkillGroup("Head", unlockSkills ? Head.Skills : Head.FindUnlockedSkills()),
			new SkillGroup("Body", unlockSkills ? Body.Skills : Body.FindUnlockedSkills()),
			new SkillGroup("Main Arm", unlockSkills ? Arm1.Skills : Arm1.FindUnlockedSkills()),
			new SkillGroup("Off Arm", unlockSkills ? Arm2.Skills : Arm2.FindUnlockedSkills())
		};

		public LimbInstance Head => instance[LimbType.Head];
		public LimbInstance Body => instance[LimbType.Body];
		public LimbInstance Arm1 => instance[LimbType.Arm1];
		public LimbInstance Arm2 => instance[LimbType.Arm2];

		public override string DNA => Body.Asset.Address.Replace(Anjin.Nanokin.Addresses.LimbPrefix + "/", "");

		public override string ToString() => instance.Name;

		public override bool has_int(string key)
		{
			return ((instance.entry != null) && instance.entry.has_int(key));
		}

		public override int load_int(string key)
		{
			if (instance.entry != null)
			{
				return instance.entry.load_int(key);
			}
			else
			{
				return 0;
			}
		}

		public override void save_int(string key, int value)
		{
			if (instance.entry != null)
			{
				instance.entry.save_int(key, value);
			}
		}

		public override void delete_int(string key)
		{
			if (instance.entry != null)
			{
				instance.entry.delete_int(key);
			}
		}

		public override void clear_ints()
		{
			if (instance.entry != null)
			{
				instance.entry.clear_ints();
			}
		}

		public override bool has_string(string key)
		{
			return ((instance.entry != null) && instance.entry.has_string(key));
		}

		public override string load_string(string key)
		{
			if (instance.entry != null)
			{
				return instance.entry.load_string(key);
			}
			else
			{
				return "";
			}
		}

		public override void save_string(string key, string value)
		{
			if (instance.entry != null)
			{
				instance.entry.save_string(key, value);
			}
		}

		public override void delete_string(string key)
		{
			if (instance.entry != null)
			{
				instance.entry.delete_string(key);
			}
		}

		public override void clear_strings()
		{
			if (instance.entry != null)
			{
				instance.entry.clear_strings();
			}
		}

		public override void SaveStats(Fighter fter)
		{
			// Copy our state in the battle to the monster data.
			// that way it carries over to the next fight if the monster data is re-used.

			instance.Points.hp = fter.points.hp / fter.max_points.hp;
			instance.Points.sp = fter.points.sp / fter.max_points.sp;
			instance.Points.op = fter.points.op;

			instance.RecalculateStats();
		}
	}
}