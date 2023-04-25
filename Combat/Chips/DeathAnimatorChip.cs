using System;
using System.Collections.Generic;
using Anjin.Scripting;
using Combat.Scripting;
using Combat.Toolkit;
using Cysharp.Threading.Tasks;
using Data.Combat;
using JetBrains.Annotations;
using MoonSharp.Interpreter;
using Pathfinding.Util;
using UnityEngine;
using UnityEngine.Assertions;

namespace Combat.Components
{
	/// <summary>
	/// Processes submitted deaths.
	/// </summary>
	public class DeathAnimatorChip : Chip
	{
		private const string ADDR_CRYSTAL = "Combat/Death Crystal";

		public Vector3 crystalPositionOffset;

		private Table _animScript;

		protected override void RegisterHandlers()
		{
			Handle(CoreOpcode.FlushDeaths, HandleFlushDeaths);
			Handle(CoreOpcode.FlushRevives, HandleRevive);
		}

		public override async UniTask InstallAsync()
		{
			await base.InstallAsync();


			LuaChangeWatcher.ClearWatches(this);
			InitializeScript();
		}

		private void InitializeScript()
		{
			LuaChangeWatcher.BeginCollecting();
			_animScript = Lua.NewScript("death-anim-crystal");
			Lua.LoadFileInto("coplayer", _animScript);
			LuaUtil.LoadBattleRequires(_animScript);
			LuaChangeWatcher.EndCollecting(this, InitializeScript);
		}

		public override void Uninstall()
		{
			base.Uninstall();

			LuaChangeWatcher.ClearWatches(this);
		}

		private void SpawnCrystal([NotNull] Fighter fighter)
		{
			battle.RemoveFighter(fighter, true); // Note: this removes the fighter from battle
			fighter.onRevive = OnRevive;
		}

		private void DespawnCrystal([NotNull] Fighter fighter)
		{
			OnRevive(fighter);
		}

		public void DespawnMonster(Fighter obj)
		{
			Assert.IsNotNull(obj.actor, "obj.View != null");
			obj.actor.gameObject.SetActive(false);
		}

		private void SpawnMonster(Fighter obj)
		{
			Assert.IsNotNull(obj.actor, "obj.View != null");
			obj.actor.gameObject.SetActive(true);
		}

		private async UniTask HandleFlushDeaths(CoreInstruction ins)
		{
			List<Fighter>    fighters   = ListPool<Fighter>.Claim();
			List<BattleAnim> animations = ListPool<BattleAnim>.Claim();

			fighters.AddRange(battle.fighters); // battle.fighters will change during iteration
			foreach (Fighter fighter in fighters)
			{
				if (fighter.points.hp > 0)
					// Already dead OR still alive.
					continue;

				animations.Add(new CoplayerAnim(_animScript, "anim_death", fighter, new object[]
				{
					(Action<Fighter>)SpawnCrystal,
					(Action<Fighter>)DespawnMonster,
				}));
			}

			if (animations.Count > 0)
			{
				runner.Submit(CoreOpcode.Execute, new CoreInstruction { actions = animations.ToArray() });
			}

			ListPool<Fighter>.Release(fighters);
			ListPool<BattleAnim>.Release(animations);
		}

		// private async UniTask HandleFlushDeathsStrong(CoreInstruction ins)
		// {
		// 	List<Fighter>      fighters   = ListPool<Fighter>.Claim();
		// 	List<BattleAction> animations = ListPool<BattleAction>.Claim();
		//
		// 	fighters.AddRange(battle.fighters); // battle.fighters will change during iteration
		// 	foreach (Fighter fighter in fighters)
		// 	{
		// 		if (fighter.death.HasValue || battle.GetPoints(fighter).hp > 0)
		// 			// Already dead OR still alive.
		// 			continue;
		//
		// 		animations.Add(new CoplayerAction(_animScript, "anim_death", fighter, new object[]
		// 		{
		// 			(Handler<Fighter>)SpawnCrystal,
		// 			(Handler<Fighter>)DespawnMonster,
		// 		}));
		// 	}
		//
		// 	if (animations.Count > 0)
		// 	{
		// 		CoreInstruction inst = new CoreInstruction();
		// 		inst.op      = CoreOpcode.Execute;
		// 		inst.actions = animations.ToArray();
		// 		core.Force(inst);
		//
		// 	}
		//
		// 	ListPool<Fighter>.Release(fighters);
		// 	ListPool<BattleAction>.Release(animations);
		// }

		private async UniTask HandleRevive(CoreInstruction ins)
		{
			List<Battle.Death> deaths     = ListPool<Battle.Death>.Claim();
			List<BattleAnim>   animations = ListPool<BattleAnim>.Claim();

			deaths.AddRange(battle.deaths); // battle.fighters will change during iteration
			foreach (Battle.Death death in deaths)
			{
				Fighter fighter = death.fighter;

				if (!fighter.reviveMarked)
					// Still alive.
					continue;

				animations.Add(new CoplayerAnim(_animScript, "anim_revive", fighter, new object[]
				{
					(Action<Fighter>)DespawnCrystal,
					(Action<Fighter>)SpawnMonster,
				}));
			}

			if (animations.Count > 0)
			{
				runner.Submit(CoreOpcode.Execute, new CoreInstruction { actions = animations.ToArray() });
			}

			ListPool<Battle.Death>.Release(deaths);
			ListPool<BattleAnim>.Release(animations);
		}

		private void OnRevive(Fighter fighter)
		{
			//throw new NotImplementedException();
			battle.ReviveFighter(fighter);
		}
	}
}