using System;
using Anjin.Util;
using UnityEngine.SceneManagement;

namespace Anjin.Utils
{
	public class MockSceneOperation : AsyncSceneOperation
	{
		private Scene _scene;

		public MockSceneOperation(Scene scene)
		{
			_scene = scene;
			IsDone = true;
		}

		public override AsyncSceneOperation OnDone(Action onDone)
		{
			onDone();
			return this;
		}

		public override AsyncSceneOperation OnScene(OnSceneLoadedCallback callback)
		{
			callback(_scene);
			return this;
		}

		public override AsyncSceneOperation OnDriver<TDriver>(Action<TDriver> callback)
		{
			TDriver driverComponent = _scene.FindRootComponent<TDriver>();
			callback(driverComponent);
			return this;
		}

		public override AsyncSceneOperation SetActive()
		{
			SceneManager.SetActiveScene(_scene);
			return this;
		}
	}
}