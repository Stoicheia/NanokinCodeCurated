using Anjin.Scripting;
using Anjin.Util;
using Combat;
using Combat.Data;
using Combat.Data.VFXs;
using Combat.Scripting;
using Combat.Toolkit;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityUtilities;
using Util.Odin.Attributes;

namespace Combat
{
	[LuaUserdata]
	[Serializable]
	public class GiantRocBrain : BattleBrain
	{
		private enum BlinkState
		{
			None,
			Hylic,
			Psychic
		}

		public const string FUNC_AI  = "ai";
		public const string API_FILE = "combat-ai-api";

		// private static List<Fighter> _allies       = new List<Fighter>();
		// private static List<Fighter> _enemies      = new List<Fighter>();
		// private static List<Team>    _scratchTeams = new List<Team>();

		[NonSerialized] public Table   env;
		[NonSerialized] public Closure function;
		[NonSerialized] public string  functionName;

		private bool initialized;

		private GiantRocAI     _ai;
		private Fighter        _self;
		private FighterActor   _actor;
		private FighterInfo    _info;
		private SpriteRenderer _sr;

		[Optional]
		[SerializeField]
		public LuaAsset Script;

		[NonSerialized]
		public string ScriptName;

		//private BlinkVFX _blinkHylicWard, _blinkPsychicWard;

		private BlinkState _blinkState;

		public GiantRocBrain(Table env, Closure function)
		{
			this.env      = env;
			this.function = function;
		}

		public GiantRocBrain(LuaAsset asset)
		{
			Script = asset;

			if (Script != null)
				ResetLua();
		}

		public GiantRocBrain(string name)
		{
			ScriptName   = "std-ai";
			functionName = name;

			if (functionName != null)
				ResetLua();
		}

		public override void OnRegistered()
		{
			initialized = false;

			_ai = new GiantRocAI
			{
				self   = _self,
				battle = battle
			};

			_blinkState = BlinkState.None;

			//_blinkHylicWard        = new BlinkVFX(10, 0.5f, Color.white, ColorsXNA.Pink);
			//_blinkHylicWard.paused = true;

			//_blinkPsychicWard        = new BlinkVFX(10, 0.5f, Color.white, ColorsXNA.Aqua);
			//_blinkPsychicWard.paused = true;

			if (env == null && Script != null)
			{
				ResetLua();
			}

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
			if (fighter.skills.Count == 0)
				return MoveOrSkip();

			// Setup
			// ----------------------------------------
			if (!initialized)
			{
				initialized = true;

				_self      = fighter;
				_actor     = _self.actor;
				_info      = _self.info;
				_ai.self   = _self;
				_ai.battle = battle;
				_sr        = _actor.gameObject.GetComponentInChildren<SpriteRenderer>();
			}

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

		public override void Update()
		{
			if (_self != null)
			{
				//if (!_info.has_string("blink"))
				//{
				//	if (_blinkState != BlinkState.None)
				//	{
				//		_blinkState = BlinkState.None;

				//		if (!_blinkHylicWard.paused)
				//		{
				//			_blinkHylicWard.paused = true;
				//			_actor.vfx.Remove(_blinkHylicWard);
				//		}

				//		if (!_blinkPsychicWard.paused)
				//		{
				//			_blinkPsychicWard.paused = true;
				//			_actor.vfx.Remove(_blinkPsychicWard);
				//		}
				//	}
				//}
				//else
				//{
				//	if (_blinkState == BlinkState.None)
				//	{
				//		string blinkType = _info.load_string("blink");
				//		_blinkState = (blinkType == "hylic" ? BlinkState.Hylic : BlinkState.Psychic);

				//		if (_blinkState == BlinkState.Hylic)
				//		{
				//			_blinkHylicWard.elapsed = 0;
				//			_blinkHylicWard.paused  = false;
				//			_actor.vfx.Add(_blinkHylicWard);
				//		}
				//		else
				//		{
				//			_blinkPsychicWard.elapsed = 0;
				//			_blinkPsychicWard.paused  = false;
				//			_actor.vfx.Add(_blinkPsychicWard);
				//		}
				//	}
				//}

				if (_actor == null)
				{
					_actor = _self.actor;
				}

				VFXState vfxstate = _actor.vfx.state;

				Color tint     = vfxstate.tint.Alpha(vfxstate.opacity);
				Color fill     = vfxstate.fill;
				float emission = vfxstate.emissionPower;

				if (_sr == null)
				{
					_sr = _actor.gameObject.GetComponentInChildren<SpriteRenderer>();
				}

				_sr.color = tint;
				_sr.ColorFill(fill);
				_sr.EmissionPower(emission);
			}
		}

		[CanBeNull]
		private BattleAnim MoveOrSkip()
		{
			if (RNG.Chance(0.075f)) // small chance to simply skip action
				return new SkipCommand().GetAction(battle);

			return MoveToRandomSlot(_self);
		}
	}
}