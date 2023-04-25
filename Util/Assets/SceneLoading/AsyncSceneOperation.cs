using System;
using Anjin.Util;
using UnityEngine.SceneManagement;

namespace Anjin.Utils
{
	public abstract class AsyncSceneOperation
	{
		protected bool setActiveOnLoad;

		public event OnSceneLoadedCallback Complete;

		public Scene LoadedScene { get; set; }

		public bool IsDone { get; set; }

		public virtual AsyncSceneOperation OnDone(Action onDone)
		{
			Complete += scene => onDone?.Invoke();
			return this;
		}

		public virtual AsyncSceneOperation OnScene(OnSceneLoadedCallback callback)
		{
			Complete += callback;
			return this;
		}

		public virtual AsyncSceneOperation OnDriver<TDriver>(Action<TDriver> callback)
		{
			Complete += scene =>
			{
				TDriver driverComponent = scene.FindRootComponent<TDriver>();
				callback(driverComponent);
			};
			return this;
		}

		protected void OnComplete(Scene scene)
		{
			if (setActiveOnLoad)
				SceneManager.SetActiveScene(scene);

			LoadedScene = scene;
			Complete?.Invoke(scene);
		}

		public virtual AsyncSceneOperation SetActive()
		{
			setActiveOnLoad = true;
			return this;
		}
	}
}