using System.IO;
using Anjin.Nanokin;
using ImGuiNET;
using UnityEngine;
using UnityEngine.InputSystem;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;

#endif

namespace Core.Debug
{
	/// <summary>
	/// Allows recording clips more easily.
	/// </summary>
	public class DebugRecorder : StaticBoy<DebugRecorder>, IDebugDrawer
	{
#if UNITY_EDITOR
		private static RecorderController         _controller;
		private static RecorderControllerSettings _recorderControllerSettings;
		private static MovieRecorderSettings      _movieSettings;
		private static bool                       _initialized;

		public static bool IsRecording => _controller.IsRecording();
#else
		public bool IsRecording => false;
#endif

		private void OnEnable()
		{
			DebugSystem.drawers.Add(this);
		}

		private static void Init()
		{
			// Note: this is pretty slow so let's not run this unless we need it
#if UNITY_EDITOR
			if (_initialized) return;
			_initialized                = true;
			_recorderControllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
			_movieSettings              = ScriptableObject.CreateInstance<MovieRecorderSettings>();
			_controller                 = new RecorderController(_recorderControllerSettings);
#endif
		}

		private void OnDisable()
		{
			DebugSystem.drawers.Remove(this);
		}

		public void OnLayout(ref DebugSystem.State state)
		{
#if UNITY_EDITOR
			Init();

			if (state.DebugMode && _controller.IsRecording())
			{
				Stop();
			}
#endif
		}

		public static void Begin()
		{
#if UNITY_EDITOR
			Init();

			EditorApplication.isPaused = true;

			string savepath = EditorUtility.SaveFilePanel("Save Recording", Path.Combine(Path.GetDirectoryName(Application.dataPath), "Recordings"), "new-recording", "webm");
			if (savepath != "")
			{
				GameController.DebugMode = false;

				_recorderControllerSettings.CapFrameRate = true;
				_recorderControllerSettings.FrameRate    = 60;

				_recorderControllerSettings.AddRecorderSettings(_movieSettings);
				_movieSettings.FrameRate         = 60;
				_movieSettings.OutputFile        = savepath;
				_movieSettings.RecordMode        = RecordMode.Manual;
				_movieSettings.FrameRatePlayback = FrameRatePlayback.Variable;
				_movieSettings.OutputFormat      = MovieRecorderSettings.VideoRecorderOutputFormat.WebM;
				_movieSettings.VideoBitRateMode  = VideoBitrateMode.Low;
				_movieSettings.CaptureAlpha      = true;
				_movieSettings.ImageInputSettings = new GameViewInputSettings
				{
					OutputWidth        = 640,
					OutputHeight       = 360,
					RecordTransparency = true
				};

				EditorApplication.isPaused = false;

				_controller.PrepareRecording();
				_controller.StartRecording();
			}

			EditorApplication.isPaused = false;
#endif
		}

		public static void Stop()
		{
#if UNITY_EDITOR
			// Auto stop recording as soon as we open debug
			_controller.StopRecording();
			Live.LogTrace("--", "Recorded a video !");
#endif
		}
	}
}