using System;
using Anjin.Util;
using UnityEngine;

public class Timer
{
	private float _dtElapsed;
	private bool  _isRegistered;
	private bool  _isDestroyed;

	private Action _onFire;
	private float  _duration;

	public Timer(Action onFire)
	{
		_onFire = onFire;
	}

	public Timer(float duration, Action onFire) : this(onFire)
	{
		Refresh(duration);
		Start();
	}


	public bool IsRunning        { get; private set; }
	public int  RepeatsLeft      { get; set; } = 1;
	public bool IsInfiniteRepeat { get; set; }
	public bool HasElapsed       => _dtElapsed > _duration;
	public bool IsFullyDone      => !IsRunning && !IsInfiniteRepeat && RepeatsLeft <= 0 || _isDestroyed;

	public void Reset()
	{
		_dtElapsed = 0;
	}

	public void Refresh(float duration)
	{
		_dtElapsed   = 0;
		_isDestroyed = false;
		_duration    = duration;
		RepeatsLeft  = RepeatsLeft.Minimum(1);
		Start();
	}

	public Timer Start()
	{
		RepeatsLeft--;
		if (!_isRegistered)
		{
			TimerManager.Instance.Register(this);
			_isRegistered = true;
		}

		IsRunning = true;
		return this;
	}

	public void Destroy()
	{
		if (IsRunning)
		{
			TimerManager.Instance.Unregister(this);
			_isRegistered = false;
		}

		_isDestroyed = true;
	}

	public void Pause()
	{
		IsRunning = false;
	}

	public void Stop()
	{
		_dtElapsed = 0;
		IsRunning  = false;
	}

	public void Resume()
	{
		IsRunning = true;
	}

	public void Complete()
	{
		_dtElapsed = _duration;
	}

	public Timer Repeat(int? repeatCount = null)
	{
		if (repeatCount.HasValue)
		{
			RepeatsLeft = repeatCount.Value;
		} else
		{
			IsInfiniteRepeat = true;
		}

		return this;
	}

	public void Update()
	{
		if (IsRunning)
		{
			_dtElapsed += Time.deltaTime;
			if (HasElapsed)
			{
				Stop();

				if (!IsFullyDone)
				{
					Start();
				}

				_onFire();
			}
		}
	}
}