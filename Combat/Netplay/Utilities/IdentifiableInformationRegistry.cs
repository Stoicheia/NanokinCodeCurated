using System.Collections.Generic;

namespace Combat
{
	public class IdentifiableInformationRegistry<TValue>
		where TValue : class, IIdentifiableInfo, new()
	{
		private Dictionary<int, TValue> _dictionary = new Dictionary<int, TValue>();

		public int ID { get; set; }

		public List<TValue> List { get; } = new List<TValue>();

		public void Clear()
		{
			_dictionary.Clear();
			List.Clear();
		}

		public TValue Update(int id)
		{
			if (!_dictionary.TryGetValue(id, out TValue value))
			{
				value = new TValue {ID = id};

				_dictionary.Add(id, value);
				List.Add(value);
			}

			return value;
		}

		public TValue this[int id] => _dictionary[id];

		public bool HasInformation(int currentId) => _dictionary.ContainsKey(currentId);
	}
}