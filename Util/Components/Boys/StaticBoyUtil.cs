
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Util.Comonents.Boys {

	[InitializeOnLoad]
	public static class StaticBoyUtil {

		static StaticBoyUtil()
		{
			EditorApplication.playModeStateChanged += PlayModeChanged;
		}

		private static void PlayModeChanged(PlayModeStateChange obj)
		{
			if (obj == PlayModeStateChange.ExitingPlayMode) {
				foreach (StaticBoyBase boy in Object.FindObjectsOfType<StaticBoyBase>()) {
					boy.Reset();
				}
			}
		}
	}
}
#endif