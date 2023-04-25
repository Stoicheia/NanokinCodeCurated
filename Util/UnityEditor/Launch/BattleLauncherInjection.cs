using System.Collections.Generic;
using Anjin.Nanokin;
using Combat.Startup;
using Cysharp.Threading.Tasks;
using Overworld.Controllers;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Util.UnityEditor.Launch;

namespace Combat.Launch
{
	[ShowInInspector]
	public class BattleLauncherInjection : SerializedMonoBehaviour
	{
		public static List<BattleLauncherInjection> injections = new List<BattleLauncherInjection>();

		[OdinSerialize]
		public BattleRecipe Recipe;

		[ShowInInspector]
		private BattleRunner _runner;

		[Button("Next")]
		private void Next()
		{
			_runner.Next();
		}

		private void OnEnable()
		{
			injections.Add(this);
		}

		private void OnDisable()
		{
			injections.Remove(this);
		}

		private void Start()
		{
			GameController.Live.OnInitialized += () =>
			{
				Launch().Forget();
			};
		}

		private void OnDestroy()
		{
			if (GameController.IsQuitting) return;
			_runner.Stop();
		}

		private async UniTaskVoid Launch()
		{
#if UNITY_EDITOR
			_runner = BattleController.CreatePlayableBattle();
			_runner.initChips.Add(new DebugEditorChip());
			_runner.initPlugins.Add(new LuaPlugin("scratch-plugin"));

			if (NanokinLauncher.Combat_FindArena(out Arena arena))
			{
				_runner.Launch(new BattleIO
				{
					recipe = Recipe,
					arena  = arena,
				});

				_runner.logClearOnRestart = true;

				GameController.Live.StateApp  = GameController.AppState.InGame;
				GameController.Live.StateGame = GameController.GameState.Battle;
				BattleController.OnExternalBattleLaunched(_runner);
			}
#endif
		}
	}
}