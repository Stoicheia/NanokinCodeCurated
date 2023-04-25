using Combat.Startup;
using Sirenix.OdinInspector;
using Util.Assets;
using Util.UnityEditor.Launch;

namespace Combat.Launch
{
	public class BattleRecipeAsset : ScriptableAsset<BattleRecipe>
	{
#if UNITY_EDITOR
		[Button(ButtonSizes.Large),
		 GUIColor(0, 1, 0),
		 PropertyOrder(-10),
		 HorizontalGroup("Content/RecipeTools")]
		public void Play()
		{
			NanokinLauncher.LaunchCombat(Value);
		}

		[Button(ButtonSizes.Large),
		 GUIColor(0.85f, 0.93f, 0.7f),
		 PropertyOrder(-10),
		 HorizontalGroup("Content/RecipeTools")]
		public void Debug()
		{
			BattleRecipeAsset ass = Instantiate(this);
			ass.Value.overridePlayerBrain = new DebugBrain();
			ass.Value.overrideEnemyBrain  = new DebugBrain();

			NanokinLauncher.LaunchCombat(ass.Value);
		}

		[Button(ButtonSizes.Large),
		 GUIColor(0.95f, 0.86f, 0.9f),
		 PropertyOrder(-10),
		 HorizontalGroup("Content/RecipeTools")]
		public void Autotest()
		{
			BattleRecipeAsset ass = Instantiate(this);
			ass.Value.overridePlayerBrain = new RandomBrain();
			ass.Value.overrideEnemyBrain  = new RandomBrain();

			NanokinLauncher.LaunchCombat(ass.Value);
		}
#endif
	}
}