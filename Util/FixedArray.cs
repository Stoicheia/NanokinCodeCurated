namespace Util
{

	/// <summary>
	/// Wraps a static fixed-length array and allows for easy filling and iteration.
	/// Intended to be used for runtime situations where you need the flexibility of a list, but
	/// don't need the heavy cost.
	///
	/// C.L: A common pattern that has been used in many places in the codebase.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class FixedArray<T> {

		private T[] _array;
		private int _size;
		private int _num;

		private T _defaultVal;

		public int  Count => _num;
		public bool Full  => _num >= _size;

		public FixedArray(int size, T defaultVal = default)
		{
			_array      = new T[size];
			_size       = size;
			_defaultVal = defaultVal;
		}

		public static implicit operator int(FixedArray<T> arr) => arr.Count;

		public T this[int index] {
			get {
				if (index >= 0 && index < _num)
					return _array[index];

				return _defaultVal;
			}
		}

		public void Reset()
		{
			_num = 0;
		}

		public bool Add(T val)
		{
			if (Full) return false;

			_array[_num] = val;
			_num++;

			return true;
		}

	}
}