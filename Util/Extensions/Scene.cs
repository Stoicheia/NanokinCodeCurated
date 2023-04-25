using System.Collections.Generic;
using Anjin.Utils;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Anjin.Util
{
	public static partial class Extensions
	{
		[CanBeNull]
		public static TComponent FindRootComponent<TComponent>(this Scene scene)
		{
			GameObject[] rootGameObjects = scene.GetRootGameObjects();
			foreach (GameObject root in rootGameObjects)
			{
				if (root.TryGetComponent(out TComponent comp))
				{
					return comp;
				}
			}

			return default;
		}

		[CanBeNull]
		public static bool FindRootComponent<TComponent>(this Scene scene, out TComponent ret)
		{
			GameObject[] rootGameObjects = scene.GetRootGameObjects();
			foreach (GameObject root in rootGameObjects)
			{
				if (root.TryGetComponent(out ret))
				{
					return true;
				}
			}

			ret = default;
			return false;
		}

		public static void SetActive(this List<GameObject> objects, bool state = true)
		{
			foreach (GameObject gameObject in objects)
			{
				gameObject.SetActive(state);
			}
		}

		public static List<GameObject> SaveActiveRoots(this Scene scene)
		{
			List<GameObject> ret = new List<GameObject>();

			GameObject[] roots = scene.GetRootGameObjects();
			foreach (GameObject root in roots)
			{
				if (root.activeSelf)
				{
					ret.Add(root);
				}
			}

			return ret;
		}

		public static void SetRootActive(this Scene scene, bool active)
		{
			GameObject[] rootObjects = scene.GetRootGameObjects();

			foreach (GameObject root in rootObjects)
			{
				root.gameObject.SetActive(active);
			}
		}

		public static void Set(this Scene scene, bool state)
		{
			if (scene.FindRootComponent(out SceneActivator activator))
			{
				activator.Set(state);
			}
		}

		public static void Clear(this Scene scene)
		{
			GameObject[] roots = scene.GetRootGameObjects();
			for (var i = 0; i < roots.Length; i++)
			{
				if (Application.isPlaying)
					Object.Destroy(roots[i]);
				else
					Object.DestroyImmediate(roots[i]);
			}
		}
	}
}