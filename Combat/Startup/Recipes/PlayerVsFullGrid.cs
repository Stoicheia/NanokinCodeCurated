using Anjin.Util;
using Assets.Nanokins;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Combat.Startup
{
	[UsedImplicitly]
	public class PlayerVsFullGrid : BattleRecipe
	{
		[InfoBox("Leave the player's team null to source it from save data instead.")]
		[Optional, CanBeNull]
		public TeamRecipe PlayerTeamRecipe = null;

		public MonsterRecipe EnemyRecipe;

		[Optional, CanBeNull]
		public BattleBrain playerBrain = new PlayerBrain();

		[Optional, CanBeNull]
		public BattleBrain enemyBrain = new RandomBrain();

		protected override async UniTask OnBake()
		{
			Team team1 = battle.AddTeam(new Team
			{
				brain    = overridePlayerBrain ?? playerBrain ?? new PlayerBrain(),
				slots    = battle.AddSlots(arena.GetSlotLayout("player")),
				isPlayer = true
			});

			Team team2 = battle.AddTeam(new Team
			{
				brain = overrideEnemyBrain ?? enemyBrain,
				slots = battle.AddSlots(arena.GetSlotLayout("enemy"))
			});

			var enemy_team = new TeamRecipe();

			for (var y = 0; y < 3; y++)
			for (var x = 0; x < 3; x++)
			{
				Slot          slot = team2.slots.all[x];
				MonsterRecipe r    = EnemyRecipe.Clone();
				r.slotcoord = new Vector2Int(x, y);
				enemy_team.Monsters.Add(r);
			}

			await UniTask.WhenAll(
				AddRecipeOrSavedata(team1, PlayerTeamRecipe),
				AddTeam(team2, enemy_team)
			);
		}
	}
}