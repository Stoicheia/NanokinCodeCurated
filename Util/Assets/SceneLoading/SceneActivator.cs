using System;
using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Anjin.Utils
{
	/// <summary>
	/// Allows defining a set of objects which will be enabled or disabled
	/// </summary>
	public class SceneActivator : MonoBehaviour
	{
		public bool            SceneRoots = true;
		public GameObject[]    Objects;
		public StartupBehavior OnStartup = StartupBehavior.Deactivate;

		// This is probably excessive but hey, to each their own!
		[NonSerialized] public Action       onBeforeActivate;
		[NonSerialized] public Action       onAfterActivate;
		[NonSerialized] public Action       onBeforeDeactivate;
		[NonSerialized] public Action       onAfterDeactivate;
		[NonSerialized] public Action<bool> onBeforeChange;
		[NonSerialized] public Action<bool> onAfterChange;

		private bool _started = false;

		public enum StartupBehavior
		{
			Untouched,
			Activate,
			Deactivate
		}

		private void Start()
		{
			if (_started) return;
			_started = true;

			switch (OnStartup)
			{
				case StartupBehavior.Activate:
					Set(true);
					break;

				case StartupBehavior.Deactivate:
					Set(false);
					break;
			}
		}

		public void Activate()
		{
			Set(true);
		}

		public void Set(bool state)
		{
			_started = true;

			onBeforeChange?.Invoke(state);

			if (state) onBeforeActivate?.Invoke();
			else onBeforeDeactivate?.Invoke();

			for (var i = 0; i < Objects.Length; i++)
			{
				Objects[i].SetActive(state);
			}

			if (SceneRoots)
			{
				foreach (GameObject go in gameObject.scene.GetRootGameObjects())
				{
					go.SetActive(state);
				}
			}

			if (state) onAfterActivate?.Invoke();
			else onAfterDeactivate?.Invoke();

			onAfterChange?.Invoke(state);
		}

		public static void Set(string name, bool state)
		{
			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (scene.name == name)
				{
					if (!scene.FindRootComponent(out SceneActivator activator))
					{
						Debug.Log($"Could not find scene activator in '{name}'. Default to disabling all ");
						return;
					}

					activator.Set(state);
					return;
				}
			}

			AjLog.LogError($"Could not find scene '{name}'.", nameof(SceneLoader), nameof(Set));
		}

#if UNITY_EDITOR
		[Title("Baking")]
		[Button]
		[LabelText("Active")]
		private void BakeActive()
		{
			Set(true);
		}

		[Button]
		[LabelText("Inactive")]
		private void BakeInactive()
		{
			Set(false);
		}
#endif
	}
}