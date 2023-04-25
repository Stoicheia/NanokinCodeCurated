using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Util.Components.Timers {


	public struct ValTimer
	{
		[ShowInInspector]
		public float time { get; private set; }

		[ShowInInspector]
		public float duration
		{
			get { return _duration; }
			set { hasDuration = true; _duration = value; }
		}

		public float norm_1to0 {
			get {
				if (done) return 0;
				return time / duration;
			}
		}

		public float norm_0to1 {
			get {
				if (done) return 1;
				return 1 - (time / duration);
			}
		}
		public bool  done      => time <= 0;

		bool  hasDuration;
		float _duration;

		public ValTimer(float duration)
		{
			time        = 0;
			hasDuration = false;
			_duration   = 0;

			this.duration = duration;
		}

		public void Set(float time = -1, bool setDuration = false)
		{
			if (time < 0)
			{
				if (hasDuration)
					this.time = duration;
				else
					Debug.LogError("Tried to set the time of a timer to its duration, when it has no set duration.");
			}
			else
			{
				this.time = Mathf.Max(0, time);
				if (setDuration) {
					duration = this.time;
				}
			}
		}

		public void Reset()
		{
			if (hasDuration)
				time = duration;
		}

		public bool Tick()
		{
			time -= Time.deltaTime;
			if (time > 0) return false;
			time = -1;
			return true;
		}

		public bool Tick(float time)
		{
			this.time -= time;
			if (this.time > 0) return false;
			time = -1;
			return true;
		}

		public static implicit operator ValTimer(float f) => new ValTimer(f);
	}
}