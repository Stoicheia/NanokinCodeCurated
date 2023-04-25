using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Recorder;
#endif

namespace Util
{
	public static class Workarounds
	{
#if UNITY_EDITOR
		public static RecorderWindow[] Recorder_GetAllWindows() => Resources.FindObjectsOfTypeAll<RecorderWindow>();

		[MenuItem("Anjin/Bullshit Workarounds/Is Unity Recorder Recording")]
		public static bool Recorder_IsRecording()
		{
			bool recording = false;

#if UNITY_EDITOR
			var windows = Recorder_GetAllWindows();
			foreach (RecorderWindow window in windows)
			{
				if (window.IsRecording())
				{
					recording = true;
					break;
				}
			}

			//Debug.Log(recording);
#endif

			return recording;
		}
#endif
	}
}