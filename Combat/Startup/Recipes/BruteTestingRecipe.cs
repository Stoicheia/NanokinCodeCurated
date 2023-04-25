using System;
using System.Linq;
using System.Text.RegularExpressions;
using Anjin.Util;
using Assets.Nanokins;
using Combat.Entities;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEngine;
using Util;

namespace Combat.Startup
{
	[Serializable]
	public class BruteTestingRecipe : BattleRecipe
	{
		// Demo nanokins by default
		[UsedImplicitly]
		public string NanokinFilter = "bigfoot|beak|catfish|puffer|peggy|flei|lanza|wup|kuchi|pengan|zoran|pudding|riding|nightshade|hamrin|wup";

		public RangeOrInt LeftTeamSize      = 3;
		public RangeOrInt RightTeamSize     = 3;
		public RangeOrInt Level             = StatConstants.MAX_LEVEL;
		public RangeOrInt Mastery           = StatConstants.MAX_MASTERY;
		public bool       ForceUnlockSkills = false;

		private int _fightLoops = 0;

		protected override async UniTask OnBake()
		{
			var allNanokins = GameAssets.Nanokins
				.Where(n => Regex.IsMatch(n.ToLower(), NanokinFilter))
				.ToList();

			var lrecipe = new TeamRecipe();
			var rrecipe = new TeamRecipe();

			Team lteam = battle.AddTeam(overridePlayerBrain, arena.GetSlotLayout("player"));
			Team rteam = battle.AddTeam(overrideEnemyBrain, arena.GetSlotLayout("enemy"));

			lteam.isPlayer = true;

			RangeOrInt leftTeamSize  = LeftTeamSize;
			RangeOrInt rightTeamSize = RightTeamSize;

			for (var i = 0; i < leftTeamSize; i++)
			{
				lrecipe.Monsters.Add(new SimpleNanokin
				{
					Nanokin = GameAssets.GetNanokin(allNanokins.Choose()),
					Level   = Level,
					Mastery = Mastery,
				});
			}

			for (var i = 0; i < rightTeamSize; i++)
			{
				rrecipe.Monsters.Add(new SimpleNanokin
				{
					Nanokin = GameAssets.GetNanokin(allNanokins.Choose()),
					Level   = Level,
					Mastery = Mastery,
				});
			}

			await UniTask.WhenAll(
				AddTeam(lteam, lrecipe),
				AddTeam(rteam, rrecipe)
			);

			foreach (Fighter fighter in battle.fighters)
			{
				if (fighter.info is NanokinInfo nanosrc)
				{
					nanosrc.unlockSkills = ForceUnlockSkills;
				}
			}

			_fightLoops = 0;
			await runner.Hook(new LoopFightChip(() =>
			{
				Debug.Log($"Fight Loop #{_fightLoops}");
			}));
		}
	}
}