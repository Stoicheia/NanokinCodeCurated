using System.Collections.Generic;
using Anjin.Nanokin.SceneLoading;
using Sirenix.OdinInspector;

namespace Anjin.Nanokin
{
	public class SceneLoaderTest : SerializedMonoBehaviour
	{
		public SceneReference 		SceneToLoad;
		public List<SceneReference> MultiScenesToLoad;

		public LevelManifest ManifestToLoad;

		public List<SceneLoadHandle> LoadHandles;

		void Awake()
		{
			LoadHandles = new List<SceneLoadHandle>();
		}

		[Button]
		public void Prune() {
			for (int i = 0; i < LoadHandles.Count; i++) {
				if (LoadHandles[i].Status.state == InstructionStatus.State.Invalid)
					LoadHandles.RemoveAt(i--);
			}
		}

		[Button]
		public void TestLoadSingleScene() => LoadHandles.Add(GameSceneLoader.LoadScene(SceneToLoad));

		[Button]
		public void TestLoadMultipleScenes()
		{
			for (int i = 0; i < MultiScenesToLoad.Count; i++) {
				LoadHandles.Add(GameSceneLoader.LoadScene(MultiScenesToLoad[i]));
			}
		}

		[Button]
		public void TestLoadManifest()
		{
			LoadHandles.Add(GameSceneLoader.LoadFromManifest(ManifestToLoad));
		}

		[Button]
		public void TestUnloadAllLoadedGroups_SingleOp() =>
			GameSceneLoader.UnloadGroups(LoadHandles.ToArray());

		[Button]
		public void TestUnloadAllLoadedGroups_MultiOps()
		{
			for (int i = 0; i < LoadHandles.Count; i++) {
				GameSceneLoader.UnloadGroups(LoadHandles[i]);
			}
		}
	}
}