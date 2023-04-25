using System.Collections.Generic;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using UnityUtilities;
using Util.Odin.Attributes;

namespace Anjin.Actors
{
	public class ActorPlayback : MonoBehaviour
	{
		[FormerlySerializedAs("System")]
		public ActorPlaybackData Data;
		public bool Loop;
		[SerializeField]
		private int PhysicsFPS = 90;

		// ----------------------------------------
		[DebugVars]
		private bool _isInvalid;
		private Actor         _character;
		private ActorRenderer _renderer;

		[Title("Recording")]
		private bool _recording;
		private float       _elapsedFrame;
		private float       _elapsedTotal;
		private float       _lastRotation;
		private List<float> _xvelBuffer, _yvelBuffer, _zvelBuffer;
		private List<float> _rotBuffer;

		[Title("Playback")]
		private bool _playing;
		private ActorPlaybackBrain _brain;
		private float              _savedTimestep;

		private float FrameDuration => 1 / Data.FPS;
		public  bool  Playing       => _playing;

		private void Awake()
		{
			_xvelBuffer = new List<float>();
			_yvelBuffer = new List<float>();
			_zvelBuffer = new List<float>();
			_rotBuffer  = new List<float>();

			_renderer  = GetComponentInChildren<ActorRenderer>(true);
			_character = GetComponentInChildren<Actor>(true);

			_isInvalid = !_renderer || !_character;
			if (_isInvalid)
			{
				this.LogError("CharacterRenderer and CharacterActor required for this component.");
			}
		}

		[TitleGroup("Editor")]
		[Button]
		[HideIf("@_recording || _playing")]
		[UsedImplicitly]
		public void Record()
		{
			if (_isInvalid) return;

			_recording    = true;
			_elapsedFrame = 0;
			_elapsedTotal = 0;
			_xvelBuffer.Clear();
			_yvelBuffer.Clear();
			_zvelBuffer.Clear();
			_rotBuffer.Clear();

			Data.Clear();
			Data.Keyframes.Add(new ActorKeyframe
			{
				Time          = 0,
				Position      = _character.Position,
				Facing        = _character.facing,
				State         = _renderer.lastAnim,
				FacingCurve   = -1,
				PositionCurve = -1 * Vector3Int.one
			});

			Time.fixedDeltaTime = 1 / (float) PhysicsFPS;
			_savedTimestep      = Time.fixedDeltaTime;
		}

		[TitleGroup("Editor")]
		[Button]
		[HideIf("@_recording || _playing")]
		[UsedImplicitly]
		public void Play()
		{
			if (_isInvalid) return;

			if (Data.FrameCount == 0)
			{
				Debug.Log("No data to playback!");
				return;
			}

			_playing    = true;
			_brain      = gameObject.AddComponent<ActorPlaybackBrain>();
			_brain.Data = Data;
			_brain.Loop = Loop;

			_character.PushOutsideBrain(_brain); // This plays the animation
		}

		[TitleGroup("Editor")]
		[Button(ButtonSizes.Large)]
		[ShowIf("@_recording || _playing")]
		[UsedImplicitly]
		public void Stop()
		{
			if (_recording)
			{
				_recording = false;
				Debug.Log($"Recorded {Data.GetHumanReadableSize()} worth of data.");

				Time.fixedDeltaTime = _savedTimestep;
			}
			else if (_playing)
			{
				_playing = false;

				_character.PopOutsideBrain(_brain);
				Destroy(_brain);
				_brain = null;
			}
		}

		private void WriteKeyframe()
		{
			if (_isInvalid) return;

			ActorPlaybackCurves curves = GameAssets.Live.ActorPlaybackCurves;

			Data.Keyframes.Add(new ActorKeyframe
			{
				Time     = _elapsedTotal,
				State    = _renderer.lastAnim,
				Position = _character.Position,
				Facing   = _character.facing,
				PositionCurve = new Vector3Int(
					curves.GetApproximateCurve(_xvelBuffer),
					curves.GetApproximateCurve(_yvelBuffer),
					curves.GetApproximateCurve(_zvelBuffer)),
				FacingCurve = GameAssets.Live.ActorPlaybackCurves.GetApproximateCurve(_rotBuffer),
			});

			_xvelBuffer.Clear();
			_yvelBuffer.Clear();
			_zvelBuffer.Clear();
			_rotBuffer.Clear();
		}

		public void FixedUpdate()
		{
			if (_isInvalid) return;

			if (_recording)
			{
				_elapsedFrame += Time.deltaTime;
				_elapsedTotal += Time.deltaTime;

				_xvelBuffer.Add(_character.Position.x);
				_yvelBuffer.Add(_character.Position.y);
				_zvelBuffer.Add(_character.Position.z);

				float newRotation = _character.facing.xz().GetAngleDeg();
				_rotBuffer.Add(_lastRotation - newRotation);
				_lastRotation = newRotation;

				if (_elapsedFrame > FrameDuration || Data.Keyframes[Data.Keyframes.Count - 1].State != _renderer.lastAnim)
				{
					_elapsedFrame = 0;
					WriteKeyframe();
				}
			}
			else if (_playing)
			{
				if (_brain.finished)
				{
					Stop();
				}
			}
		}
	}
}