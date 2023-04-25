using UnityEngine;
using UnityEngine.SceneManagement;

namespace Anjin.Utils
{
	public class SceneUnloading : AsyncSceneOperation
	{
		public SceneUnloading(string sceneName)
		{
			if (!SceneManager.GetSceneByName(sceneName).isLoaded)
				return;

			AsyncOperation asyncOperation = SceneManager.UnloadSceneAsync(sceneName);
			asyncOperation.completed += a =>
			{
				IsDone = true;
				OnComplete(new Scene());
			};
		}
	}
}