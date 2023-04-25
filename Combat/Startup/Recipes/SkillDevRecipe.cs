using Anjin.Nanokin;
using Anjin.Util;
using Cysharp.Threading.Tasks;

namespace Combat.Startup
{
	public class SkillDevRecipe : PlayerOpponent1V1
	{
		public BattleRecipe internalRecipe;

		protected override async UniTask OnBake()
		{
			if (internalRecipe != null)
			{
				internalRecipe.runner         = runner;
				internalRecipe.loadingBatch = loadingBatch;

				internalRecipe.overridePlayerBrain = overridePlayerBrain ?? playerBrain;
				internalRecipe.overrideEnemyBrain  = overrideEnemyBrain ?? enemyBrain;
				internalRecipe.overrideBrain       = overrideBrain;
				await internalRecipe.Bake();
			}
			else
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

				await UniTask.WhenAll(
					AddRecipeOrSavedata(team1, PlayerTeamRecipe),
					AddTeam(team2, EnemyTeamRecipe)
				);
			}

			// Enable those flags so we can continuously test the skill!
			GameOptions.current.combat_use_cost.Value = false;
			GameOptions.current.combat_deaths.Value   = false;
			// GameOptions.current.combat_use_loop.Value = true;

			GameController.DebugMode = true;
		}
	}
}