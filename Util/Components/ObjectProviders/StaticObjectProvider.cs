using System.Collections.Generic;
using UnityEngine;

namespace Util.ObjectProviders
{
	public sealed class StaticObjectProvider : ObjectProvider
	{
		[SerializeField] private GameObject[] _objects;
		[SerializeField] private bool         _deactivateAllOnAwake;

		private int _currentIndex;

		public override    bool                    HasRoom    => _currentIndex < _objects.Length;
		protected override IEnumerable<GameObject> AllObjects => _objects;

		private void Awake()
		{
			if (_deactivateAllOnAwake)
			{
				foreach (GameObject go in _objects)
				{
					go.SetActive(false);
				}
			}
		}

		public override GameObject GetNext()
		{
			GameObject ret = _objects[_currentIndex];
			ret.SetActive(true);

			_currentIndex++;
			return ret;
		}

		public override GameObject Get(int index)
		{
			return _objects[index];
		}
	}
}