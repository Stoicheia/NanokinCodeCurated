using System;
using UnityEngine.SceneManagement;

namespace Util
{
	public static class SceneUtil
	{
		public static bool All(Func<Scene, bool> predicate)
		{
			for (var i = 0; i < SceneManager.sceneCount; i++)
			{
				Scene scene = SceneManager.GetSceneAt(i);
				if (!predicate(scene))
					return false;
			}

			return true;
		}
	}
}