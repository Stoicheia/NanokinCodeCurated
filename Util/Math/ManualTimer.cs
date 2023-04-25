using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Util
{
	/// <summary>
	/// A timer which has to be manually updated.
	/// Can be used to simplify various timing logic.
	/// </summary>
	[Serializable]
	public class ManualTimer
	{
		[SerializeField, FormerlySerializedAs("duration")] public RangeOrFloat Duration;

		[Title("Debug")]
		[ShowInInspector, HideInEditorMode, ReadOnly] private float _currentDuration;
		[ShowInInspector, HideInEditorMode, ReadOnly] private float _elapsed;

		[PropertySpace]
		[ShowInInspector, HideInEditorMode, ReadOnly] public bool IsPlaying { get; private set; }

		[ShowInInspector, HideInEditorMode, ReadOnly] public bool JustEnded { get; private set; }
		[ShowInInspector, HideInEditorMode, ReadOnly] public bool IsDone    => _elapsed >= _currentDuration;

		public bool IsDisabled => !Duration.IsRange;

		public float Elapsed       => _elapsed;
		public float RemainingTime => _currentDuration - _elapsed;
		public float Progress      => _elapsed / _currentDuration;

		public ManualTimer()
		{ }

		public ManualTimer(float duration)
		{
			Duration = duration;
		}

		public ManualTimer(RangeOrFloat duration)
		{
			Duration = duration;
		}

		public ManualTimer(float min, float max)
		{
			Duration = new RangeOrFloat(min, max);
		}

		public void Reset()
		{
			_currentDuration = Duration.Evaluate();
			_elapsed         = 0;
			JustEnded        = false;
			IsPlaying        = false;
		}

		public void Restart()
		{
			Reset();
			IsPlaying = true;
			if (IsDone) JustEnded = true;
		}

		public void PlayOrContinue()
		{
			if (IsPlaying)
				return;

			Reset();
			IsPlaying = true;
			if (IsDone) JustEnded = true;
		}

		public void Stop()
		{
			IsPlaying = false;
		}

		public bool Update()
		{
			return Update(Time.deltaTime);
		}

		public bool Update(float dt)
		{
			if (!IsPlaying)
				return false;

			if (IsDone)
			{
				_elapsed  = 0;
				JustEnded = false;
				IsPlaying = false;
				return true;
			}
			else
			{
				_elapsed += dt;
				if (IsDone) JustEnded = true;
				return false;
			}
		}

		public void Finish()
		{
			_elapsed = _currentDuration;
		}

		public static implicit operator ManualTimer(float duration)
		{
			return new ManualTimer(duration);
		}

		public static implicit operator ManualTimer((float min, float max) duration)
		{
			return new ManualTimer(duration.min, duration.max);
		}
	}
}