using System;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Util.Odin.Attributes;

namespace Combat.Startup
{
	/// <summary>
	/// Recipe for a 1V1 between a player and another team, usually controlled by AI.
	/// Leave the player team null to source it from the current save data.
	/// </summary>
	[Serializable]
	public class PlayerOpponent1V1 : BattleRecipe
	{
		[InfoBox("Leave the player's team null to source it from save data instead.")]
		[Optional, CanBeNull]
		public TeamRecipe PlayerTeamRecipe = null;

		[Required, NotNull]
		public TeamRecipe EnemyTeamRecipe = new TeamRecipe();

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

			Coach coach = null;

			if (sharedCoachPrefab != null)
			{
				coach = new NanokeeperCoach(null, sharedCoachPrefab);
			}

			Team team2 = battle.AddTeam(new Team
			{
				brain = overrideEnemyBrain ?? enemyBrain,
				slots = battle.AddSlots(arena.GetSlotLayout("enemy")),
				coach = coach
			});

			await UniTask.WhenAll(
				AddRecipeOrSavedata(team1, PlayerTeamRecipe),
				AddTeam(team2, EnemyTeamRecipe)
			);
		}
	}
}