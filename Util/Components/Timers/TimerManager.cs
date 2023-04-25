using System.Collections.Generic;
using Sirenix.OdinInspector;

public class TimerManager : SerializedMonoBehaviour
{
	public static TimerManager Instance { get; private set; }

	[ShowInInspector] private List<Timer> _registeredTimers;
	private                   List<Timer> _timersToAdd;
	private                   List<Timer> _timersToKill = new List<Timer>();

	private void Awake()
	{
		//DontDestroyOnLoad(gameObject);
		Instance          = this;
		_registeredTimers = new List<Timer>();
		_timersToAdd      = new List<Timer>();
	}

	public void Register(Timer   tim) => _timersToAdd.Add(tim);
	public void Unregister(Timer tim) => _timersToKill.Add(tim);

	private void Update()
	{
		_registeredTimers.AddRange(_timersToAdd);
		_timersToAdd.Clear();

		foreach (Timer timer in _registeredTimers)
		{
			timer.Update();

			if (timer.IsFullyDone)
			{
				_timersToKill.Add(timer);
			}
		}

		foreach (Timer toKill in _timersToKill)
		{
			_registeredTimers.Remove(toKill);
		}

		_timersToKill.Clear();
	}
}