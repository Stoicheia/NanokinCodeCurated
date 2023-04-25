using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Util
{
	[Serializable]
	public class GameObjectListPool<TComponent>
		where TComponent : Component
	{
		[SerializeField] private GameObjectPool _pool;

		/// <summary>
		/// Indicate whether transforms managed by this pool should have their sibling index set according to their position in the list pool.
		/// </summary>
		[SerializeField] private bool _withSiblingIndices;

		private List<TComponent> _objects;

		public List<TComponent> Reserve(IList list)
		{
			return Reserve(list.Count);
		}

		public List<TComponent> Reserve(int count)
		{
			if (_objects == null)
			{
				_objects = new List<TComponent>();
			}


			while (_objects.Count < count)
			{
				TComponent obj = _pool.GetAndLock<TComponent>();
				_objects.Add(obj);
			}

			for (int i = _objects.Count - 1; _objects.Count > count; i--)
			{
				TComponent obj = _objects[i];
				_pool.ReleasePoolee(obj);
				_objects.RemoveAt(i);
			}

			if (_withSiblingIndices)
			{
				// This decreases the performance of the function to O(N) performance with this.
				// Perhaps we can wrap this with a preprocessor directive since it's more a debugging utility anyway?
				// Could end up costly if we use this function a lot and the pool is large.

				for (int i = 0; i < _objects.Count; i++)
				{
					TComponent component = _objects[i];
					component.transform.SetSiblingIndex(i);
				}
			}

			return _objects;
		}

		public TComponent this[int i] => _objects[i];
	}
}