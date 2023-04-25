using UnityEngine;
using UnityEngine.SceneManagement;

namespace Anjin.Utils
{
	public class SceneLoading : AsyncSceneOperation
	{
		private AsyncOperation _unityOperation;

		public SceneLoading(string sceneName, LoadSceneMode loadMode)
		{
			_unityOperation = SceneManager.LoadSceneAsync(sceneName, loadMode);
			_unityOperation.completed += a =>
			{
				IsDone = true;

				Scene loadedScene = SceneManager.GetSceneByName(sceneName);
				OnComplete(loadedScene);
			};
		}
	}
}