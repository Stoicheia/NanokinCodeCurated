using System;
using Anjin.Scripting;
using Combat.Data;
using Combat.Scripting;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using UnityEngine;
using Util.Odin.Attributes;

namespace Combat
{
	// IDEA Prioritize by level (high level need)
	[LuaUserdata]
	[Serializable]
	public class UtilityAIBrain : BattleBrain
	{
		public const string FUNC_AI  = "ai";
		public const string API_FILE = "combat-ai-api";

		// private static List<Fighter> _allies       = new List<Fighter>();
		// private static List<Fighter> _enemies      = new List<Fighter>();
		// private static List<Team>    _scratchTeams = new List<Team>();

		[NonSerialized] public Table   env;
		[NonSerialized] public Closure function;
		[NonSerialized] public string  functionName;

		private UtilityAI _ai;
		private Fighter   _self;

		[Optional]
		[SerializeField]
		public LuaAsset Script;

		[NonSerialized]
		public string ScriptName;

		public UtilityAIBrain(Table env, Closure function)
		{
			this.env      = env;
			this.function = function;
		}

		public UtilityAIBrain(LuaAsset asset)
		{
			Script = asset;

			if (Script != null)
				ResetLua();
		}

		public UtilityAIBrain(string name)
		{
			ScriptName   = "std-ai";
			functionName = name;

			if (functionName != null)
				ResetLua();
		}

		public override void OnRegistered()
		{
			_ai = new UtilityAI
			{
				self   = _self,
				battle = battle
			};

			// battle.AddTrigger(ev =>
			// {
			// 	ev.
			// }, new Trigger
			// {
			// 	filter = self
			// });
		}

		public override void Cleanup()
		{
			base.Cleanup();
			LuaChangeWatcher.ClearWatches(this);
		}

		private void ResetLua()
		{
			LuaChangeWatcher.BeginCollecting();
			{
				// Framework and resources
				env = Lua.NewEnv("battle-ai");
				Lua.LoadFileInto(API_FILE, env);

				// Load our script
				UpdateGlobals();

				if (Script != null)
					Lua.LoadAssetInto(Script, env);
				else if (ScriptName != null)
					Lua.LoadFileInto(ScriptName, env);
			}
			LuaChangeWatcher.EndCollecting(this, ResetLua);
		}

		private void UpdateGlobals()
		{
			env[LuaEnv.OBJ_BRAIN] = _ai;
		}

		public override BattleAnim OnGrantAction()
		{
			// Setup
			// ----------------------------------------
			_self      = fighter;
			_ai.self   = fighter;
			_ai.battle = battle;

			if (fighter.skills.Count == 0)
				return MoveOrSkip();

			// update option buffers
			if (env != null)
			{
				_ai.Reset();
				UpdateGlobals();

				if (function != null)
					Lua.Invoke(function);
				else
					Lua.Invoke(env, functionName ?? FUNC_AI, optional: true);
			}

			// _ai.OnTurnStart();

			// Decision process
			// ----------------------------------------
			_ai.PopulateSolutions();

			AISolution sol = _ai.Decide();

			_ai.OnSolutionChosen(sol);

			var targeting = new Targeting();
			targeting.AddPick(sol.target);

			switch (sol.action)
			{
				case AIAction.skill: return new SkillAnim(_self, sol.skill, targeting);
				case AIAction.move:  return new MoveAnim(_self, sol.target.Slot, MoveSemantic.Auto);
				case AIAction.hold:  return new HoldAnim(_self);
				default:             throw new ArgumentOutOfRangeException();
			}
		}

		[CanBeNull]
		private BattleAnim MoveOrSkip()
		{
			if (RNG.Chance(0.075f)) // small chance to simply skip action
				return new SkipCommand().GetAction(battle);

			return MoveToRandomSlot(_self);
		}


		private UtilityAI.Need FindMostUrgent(float ally_hp_percent, float enemy_hp_percent) =>
			// BattleAI.Need most_urgent = new BattleAI.Need
			// {
			// 	type = BattleAI.NeedType.None
			// };
			//
			// foreach (BattleAI.Need need in ai.needs)
			// {
			// 	// Skip needs that are empty for whatever reason
			// 	if (need.type == BattleAI.NeedType.None || need.target == BattleAI.NeedTarget.None) continue;
			//
			// 	float weight = need.weight_normalized;
			//
			// 	switch (need.type)
			// 	{
			// 		case BattleAI.NeedType.high_hp:
			// 			weight += high_hp_need(GetHPPercentForNeed(need, self, ally_hp_percent, enemy_hp_percent));
			// 			break;
			//
			// 		case BattleAI.NeedType.low_hp:
			// 			weight += low_hp_need(GetHPPercentForNeed(need, self, ally_hp_percent, enemy_hp_percent));
			// 			break;
			// 	}
			//
			// 	Debug.Log($"Need: {need}, weight normalized: {need.weight_normalized}, weight after factor in: {weight}");
			//
			// 	if (most_urgent.type == BattleAI.NeedType.None || weight > most_urgent.weight)
			// 	{
			// 		most_urgent = need;
			// 	}
			// }
			//
			// return most_urgent;
			default;


		// private float GetAllyHPPercent(bool no_allies)
		// {
		// 	if (!no_allies)
		// 	{
		// 		float sum = 0;
		// 		foreach (Fighter ally in _allies)
		// 		{
		// 			sum += ally.hp_percent;
		// 		}
		//
		// 		return sum / _allies.Count;
		// 	}
		// 	else
		// 	{
		// 		// Default to self if no allies
		// 		return self.hp_percent;
		// 	}
		// }

		// private float GetEnemyHPPercent()
		// {
		// 	float total = 1;
		// 	float sum   = 0;
		//
		// 	foreach (Fighter enemy in _enemies)
		// 		sum += enemy.hp_percent;
		//
		// 	total = sum / _allies.Count;
		//
		// 	return total;
		// }

		public float high_hp_need(float hp_percent) => 1f - Mathf.Clamp01(hp_percent);

		public float low_hp_need(float hp_percent) => Mathf.Clamp01(hp_percent);

		// public float high_hp_need(float hp_percent)
		// {
		// 	const float high_hp_threshold = 0.9f;
		// 	if (hp_percent > high_hp_threshold) return 0;
		// 	return Mathf.Exp(1 - Mathf.Clamp01(hp_percent - high_hp_threshold));
		// }

		private float GetHPPercentForNeed(UtilityAI.Need need, Fighter fighter, float ally_hp_percent, float enemy_hp_percent)
		{
			switch (need.target)
			{
				case UtilityAI.AITarget.self:  return fighter.hp_percent;
				case UtilityAI.AITarget.ally:  return ally_hp_percent;
				case UtilityAI.AITarget.enemy: return enemy_hp_percent;
				case UtilityAI.AITarget.exact: throw new NotImplementedException(); // TODO
			}

			return 0;
		}

		// private bool RandomSkillTargetingFighter(Fighter victim, [CanBeNull] out BattleAction action)
		// {
		// 	action = null;
		//
		// 	List<SkillOption> targeting = _options.Where(x => x.target.fighters.Contains(self)).ToList();
		//
		// 	if (targeting.Count <= 0) return false;
		// 	this.Log($"{self}: Targeting weakest fighter lower than our level ({victim}). Number of options: {targeting.Count}");
		// 	SkillOption opt = targeting.Choose();
		//
		// 	var finalTargeting = new Targeting();
		// 	finalTargeting.AddPick(opt.target);
		//
		// 	action = new SkillCommand(self, opt.skill, finalTargeting).GetAction(battle);
		// 	return true;
		// }

		// Fighter weakest = null;
		// foreach (Fighter opponent in _opponents)
		// {
		// 	if (weakest == null || opponent.level.Value < weakest.level.Value)
		// 	{
		// 		weakest = opponent;
		// 	}
		// }
		//
		// if (weakest != null && weakest.level.Value < self.level.Value)
		// {
		// 	List<SkillOption> targeting_weakest = _options.Where(x => x.target.fighters.Contains(weakest)).ToList();
		//
		// 	if (RandomSkillTargetingFighter(weakest, out var action))
		// 	{
		// 		this.Log($"{self}: Targeting weakest fighter lower than our level ({weakest}). Number of options: {targeting_weakest.Count}");
		// 		return action;
		// 	}
		//
		// 	this.Log($"{self}: No skills found to target weakest, moving to random slot.");
		// 	return MoveToRandomSlot(self);
		// }
	}
}