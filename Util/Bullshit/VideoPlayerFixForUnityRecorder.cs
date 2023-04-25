using UnityEngine;
using UnityEngine.Video;

namespace Util
{
	public class VideoPlayerFixForUnityRecorder : MonoBehaviour
	{
		private VideoPlayer Player;

		private void Awake()
		{
			Player = GetComponent<VideoPlayer>();
		}

		private void Update()
		{
			if (
#if UNITY_EDITOR
				!Workarounds.Recorder_IsRecording() ||
#endif
				!Player) return;

			// NOTE(C.L): This is to fix the fact that Video Players do not maintain a consistent framerate when the Unity Recorder is running.

			Player.Pause();
			if (Player.frame / Player.frameRate < Time.time)
			{
				Player.StepForward();
				if (Player.frame >= (long)Player.frameCount - 1)
				{
					Player.Stop();
					Player.Prepare();
					Player.Play();
				}
			}
		}
	}
}