using System;
using Anjin.Nanokin;
using Anjin.Nanokin.SceneLoading;
using Anjin.Scripting;
using Anjin.Utils;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using Util.Odin.Attributes;

namespace Overworld.UI {
	public class SplashScreenSpawner : SerializedMonoBehaviour {

		[AddressFilter("SplashScreens/")]
		public string Address;
		public bool SpawnOnStart = false;
		public bool UnloadSceneOnFinish = false;

		private async void Start()
		{
			await GameController.TillIntialized();
			//await Lua.initTask;

			if(SpawnOnStart) {
				await UniTask.WaitUntil(() => SplashScreens.Exists);
				SplashScreens.ShowPrefab(Address, OnFinish);
			}
		}

		[Button]
		public async void Spawn()
		{
			await GameController.TillIntialized();
			//await Lua.initTask;
			SplashScreens.ShowPrefab(Address, OnFinish);
		}

		public void OnFinish()
		{
			if (!UnloadSceneOnFinish) return;
			GameSceneLoader.UnloadScenes(gameObject.scene);
		}
	}
}