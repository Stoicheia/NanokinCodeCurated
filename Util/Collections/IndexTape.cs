using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Util.Collections
{
	/// <summary>
	/// A utility class to traverse a list over time.
	/// The classic pattern of keeping an index for sampling a collection
	/// and incrementing it at specific moments.
	/// </summary>
	/// <typeparam name="TValue"></typeparam>
	public struct IndexTape<TValue>
	{
		[ShowInInspector] public  int           index;
		[ShowInInspector] private IList<TValue> _tape;

		public TValue Current => _tape[index];

		/// <summary>
		/// Gets a flag indicating whether or not the header has scrolled past the limits.
		/// </summary>
		public bool HasHeaderItem => _tape != null && index < _tape.Count;

		/// <summary>
		/// Gest a flag indicating whether or not there is content left in the tape.
		/// </summary>
		public bool HasMoreRemaining => _tape != null && index < _tape.Count - 1;

		public IList<TValue> All => _tape;

		public IndexTape(IList<TValue> tape) : this()
		{
			index = 0;
			_tape        = tape;

			if (_tape == null)
			{
				Debug.LogError("The tape is null!");
			}
		}

		[ButtonGroup("buttons")]
		public void Step()
		{
			index++;
		}

		public TValue Pop()
		{
			TValue currentValue = Current;
			index++;
			return currentValue;
		}

		public TValue PopAndStep()
		{
			TValue currentValue = Current;
			index++;
			return currentValue;
		}

		public void RunToCompletion(Action<TValue> action = null)
		{
			while (HasHeaderItem)
			{
				TValue current = Pop();
				action?.Invoke(current);
			}
		}
	}
}