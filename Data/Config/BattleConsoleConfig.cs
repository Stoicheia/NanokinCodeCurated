using System;
using System.Collections.Generic;
using Assets.Nanokins;
using Combat;
using Combat.Launch;
using UnityEngine.Serialization;

[Serializable]
public class BattleConsoleConfig
{
	[FormerlySerializedAs("recipe")]         public BattleRecipeAsset   Recipe;
	[FormerlySerializedAs("playerNanokins")] public List<SimpleNanokin> PlayerNanokins = new List<SimpleNanokin>();
	[FormerlySerializedAs("enemyNanokins")]  public List<SimpleNanokin> EnemyNanokins  = new List<SimpleNanokin>();
	[FormerlySerializedAs("playerUnits")]    public List<MonsterRecipe> PlayerUnits    = new List<MonsterRecipe>();
	[FormerlySerializedAs("enemyUnits")]     public List<MonsterRecipe> EnemyUnits     = new List<MonsterRecipe>();

	[FormerlySerializedAs("enemyControllerNanokin")] public BattleBrain playerBrainNanokin = new RandomBrain();
	[FormerlySerializedAs("enemyControllerNanokin")] public BattleBrain enemyBrainNanokin  = new RandomBrain();
	[FormerlySerializedAs("enemyControllerUnit")]    public BattleBrain enemyBrainUnit     = new RandomBrain();

	// public List<SaveData.FormationEntry> PlayerFormation = new List<SaveData.FormationEntry>();
	// public List<SaveData.FormationEntry> EnemyFormation  = new List<SaveData.FormationEntry>();
}