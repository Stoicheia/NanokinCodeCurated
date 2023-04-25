using System;


public class Lazy<TValue>
{
	private TValue       _value;
	private Func<TValue> _valueGetter;
	private bool         _isInvalidated = true;

	public Lazy(Func<TValue> valueGetter)
	{
		_valueGetter = valueGetter;
	}

	public TValue Value => _isInvalidated
							   ? Update()
							   : _value;

	public event Action<TValue> Updated;

	public TValue Update()
	{
		_value         = _valueGetter();
		_isInvalidated = false;
		Updated?.Invoke(_value);
		return _value;
	}

	public void Invalidate()
	{
		_isInvalidated = true;
	}

	public static implicit operator TValue(Lazy<TValue> lazy)
	{
		return lazy.Value;
	}
}