using System;
using System.Collections.Generic;
using Anjin.Util;
using JetBrains.Annotations;
using UnityEngine;
using Util;

namespace Combat.UI.TurnOrder
{
	/// <summary>
	/// A pool for the action views, a bit particular. TODO document more in detail
	/// </summary>
	public class ViewPool
	{
		private readonly DictionaryReader<GameObject, List<PooledView>> _poolsByPrefab;
		private          HashSet<PooledView>                            _inactivePrefabs;
		private          HashSet<PooledView>                            _activePrefabs;

		public ViewPool()
		{
			_poolsByPrefab = new StaticDictionaryReader<GameObject, List<PooledView>>(
				new Dictionary<GameObject, List<PooledView>>(),
				() => throw new NotImplementedException(),
				prefab => new List<PooledView>()
			);

			_inactivePrefabs = new HashSet<PooledView>();
			_activePrefabs   = new HashSet<PooledView>();
		}

		public event Action<PooledView> Allocated;

		public Transform ObjectParent { get; set; }

		public PooledView GetAndLock(GameObject prefab)
		{
			List<PooledView> pool = _poolsByPrefab[prefab].ValueOrCreate;

			if (pool.Count == 0)
				// The pool has run out.
				Allocate(prefab);

			PooledView ret = pool[0];
			pool.RemoveAt(0);

			_activePrefabs.Add(ret);
			_inactivePrefabs.Remove(ret);

			ret.gameObject.SetActive(true);
			return ret;
		}

		public void Release(PooledView pooledToRelease)
		{
			if (_inactivePrefabs.Contains(pooledToRelease))
				// Already released.
				return;

			List<PooledView> pool = _poolsByPrefab[pooledToRelease.prefab].ValueOrCreate;
			pool.Add(pooledToRelease);

			_activePrefabs.Remove(pooledToRelease);
			_inactivePrefabs.Add(pooledToRelease);

			pooledToRelease.gameObject.SetActive(false);
		}

		public void Release([NotNull] List<PooledView> toRelease)
		{
			foreach (PooledView pooledView in toRelease)
			{
				Release(pooledView);
			}
		}

		public void ReleaseAll()
		{
			foreach (PooledView prefabing in _activePrefabs)
			{
				_inactivePrefabs.Add(prefabing);
				_poolsByPrefab[prefabing.prefab].ValueOrCreate.Add(prefabing);
				prefabing.gameObject.SetActive(false);
			}

			_activePrefabs.Clear();
		}

		private void Allocate(GameObject prefab)
		{
			List<PooledView> availableInstances = _poolsByPrefab[prefab].ValueOrCreate;

			var prefabing = new PooledView(prefab, ObjectParent);
			availableInstances.Add(prefabing);

			Allocated?.Invoke(prefabing);
		}
	}
}