using System.Collections.Generic;
using System.Linq;
using Anjin.Scripting;
using Combat;
using Combat.Data;
using Combat.StandardResources;
using Combat.Toolkit;
using Data.Combat;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Puppets.Assets;

[UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
public class SkillEvent : TriggerEvent
{
	public BattleSkill instance;

	public SkillAnim anim;

	public bool overdrive = false;

	[NotNull]
	public string name => asset.name.ToLower();

	[NotNull]
	public string displayname => asset.DisplayName.ToLower();

	public SkillAsset asset => instance.asset;

	public ScriptableLimb limb => instance.limb;

	// public int affected => TargetEffectList?.Count ?? 0;

	public bool hurts;
	public bool heals;
	public bool physical;
	public bool magical;

	// public List<(object, ProcContext)> TargetEffectList = new List<(object, ProcContext)>();

	//public Fighter protectee { get; private set; }
	//public Fighter protector { get; private set; }

	public SkillEvent(Fighter me, BattleSkill instance, SkillAnim anim)
	{
		this.me       = me;
		this.instance = instance;
		this.anim = anim;

		//protectee = null;
		//protector = null;
	}

	public override void Reset()
	{
		base.Reset();
		//protectee = null;
		//protector = null;
	}

	[NotNull]
	public List<Fighter> damage_victims([CanBeNull] DynValue filter = null)
	{
		HashSet<Elements> allowedElements = new HashSet<Elements>();

		if (filter != null)
		{
			if (filter.AsString(out string s))
			{
				allowedElements = ElementsUtil.String2Elements(s);
			}

			else if (filter.AsTable(out Table t))
			{
				foreach (var value in t.Values)
				{
					allowedElements.Add(ElementsUtil.String2Element(value.AsString()));
				}
			}
		}
		else
		{
			allowedElements = ElementsUtil.AllElements();
		}

		HashSet<Fighter> results = new HashSet<Fighter>();
		Fighter caster = me as Fighter;
		// TODO
		// foreach (var pair in TargetEffectList)
		// {
		// 	Fighter fter = pair.Item1 as Fighter;
		// 	ProcContext eff = pair.Item2;
		// 	if(fter == null) continue;
		// 	if (fter.team == caster.team) continue;
		// 	if (!eff.hurts) continue;
		// 	if (!eff.get_elements().Overlaps(allowedElements)) continue;
		//
		// 	results.Add(fter);
		// }

		return results.ToList();
	}

	public bool chose([NotNull] DynValue val)
	{
		Fighter weakest = val.UserData.Object as Fighter;

		if (weakest != null)
		{
			switch (me)
			{
				case Targeting targeting: return (targeting.fighters.Contains(weakest) || (targeting.slots.Find(x => x.owner == weakest) != null));
				case Target target: return (target.fighters.Contains(weakest) || (target.slots.Find(x => x.owner == weakest) != null));
				case Fighter fighter: return (fighter == weakest);
				default:
					return false;
			}
		}
		else
		{
			return false;
		}
	}

	public bool matches(string name)
	{
		return this.name == name;
	}

	public bool matches(SkillAsset asset)
	{
		return this.asset == asset;
	}

	public bool matches_any([NotNull] Table tbl)
	{
		for (var i = 1; i <= tbl.Length; i++)
		{
			DynValue val = tbl.Get(i);
			if (name == val.String)
			{
				return true;
			}
		}

		return false;
	}

	public int sp_cost()
	{
		return asset.EvaluateInfo().spcost;
	}

	public int hp_cost()
	{
		return asset.EvaluateInfo().hpcost;
	}

	public int cost()
	{
		return instance.Cost();
	}

	public void redirect(Fighter protector)
	{
		anim.RedirectVictim(protector);

		//protectee = old;
		//protector = redirect;
	}
}