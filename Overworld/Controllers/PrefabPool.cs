using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Util;
using Util.Addressable;

namespace Overworld.Controllers
{
	public class PrefabPool : StaticBoy<PrefabPool>, IPool
	{
		[SerializeField] private List<PrimingEntry> StartupPriming;

		private Dictionary<GameObject, GObjectPool>                  _poolByPrefab;
		private Dictionary<GameObject, GObjectPool>                  _poolByInstance;
		private Dictionary<string, GObjectPool>                      _poolByAddress;
		private Dictionary<string, AsyncOperationHandle<GameObject>> _handles;

		protected override void OnAwake()
		{
			base.OnAwake();

			_poolByPrefab   = new Dictionary<GameObject, GObjectPool>();
			_poolByInstance = new Dictionary<GameObject, GObjectPool>();
			_poolByAddress  = new Dictionary<string, GObjectPool>();
			_handles        = new Dictionary<string, AsyncOperationHandle<GameObject>>();

			if (!GameOptions.current.pool_on_demand)
			{
				foreach (PrimingEntry entry in StartupPriming)
				{
					Prime(entry.Prefab, entry.Count);
				}
			}
		}

		/// <summary>
		/// Get an instance from the pool.
		/// </summary>
		[NotNull]
		public static GameObject Rent([NotNull] GameObject prefab)
		{
			if (!Live._poolByPrefab.TryGetValue(prefab, out GObjectPool pool))
			{
				Live._poolByPrefab.Add(prefab, pool = new GObjectPool(Live.transform, prefab)
				{
					maxSize      = 512,
					allocateTemp = true,
					safetyChecks = false
				});
			}

			// We could technically keep _poolByInstance populated and only remove records
			// when the instance is fully destroyed (e.g. with Wipe()).
			// Something to consider in the future if the prefab pool gets utterly abused
			// through some feature in the game.
			//
			// If we do it, we need to move Pool.Allocate() into here to completely avoid
			// touching _poolByInstance in PrefabPool.Get() and PrefabPool.Free()

			GameObject instance = pool.Rent();
			Live._poolByInstance[instance] = pool;

			return instance;
		}

		public static TComponent Rent<TComponent>([NotNull] TComponent prefab, Transform parent)
			where TComponent : Component
		{
			return Rent(prefab.gameObject, parent).GetComponent<TComponent>();
		}

		public static TComponent Rent<TComponent>([NotNull] GameObject prefab, Transform parent)
			where TComponent : Component
		{
			return Rent(prefab, parent).GetComponent<TComponent>();
		}


		[NotNull]
		public static GameObject Rent([NotNull] GameObject prefab, Transform parent)
		{
			GameObject obj = Rent(prefab);
			obj.transform.SetParent(parent, false);
			return obj;
		}

		[NotNull]
		public static GameObject Rent([NotNull] GameObject prefab, Vector3 pos)
		{
			GameObject rent = Rent(prefab);
			rent.transform.position = pos;
			return rent;
		}

		[NotNull]
		public static GameObject Rent([NotNull] GameObject prefab, Vector3 position, Quaternion rotation)
		{
			GameObject obj = Rent(prefab);

			Transform objTransform = obj.transform;
			if(Live._poolByPrefab.TryGetValue(prefab, out var pool))
				objTransform.SetParent(pool.root);
			objTransform.rotation = rotation;
			objTransform.position = position;

			return obj;
		}

		[NotNull]
		public static GameObject Rent([NotNull] GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
		{
			GameObject obj = Rent(prefab);

			Transform objTransform = obj.transform;
			objTransform.SetParent(parent, false);
			objTransform.rotation = rotation;
			objTransform.position = position;

			return obj;
		}

		public static void ReturnSafe<TComponent>([CanBeNull] TComponent comp)
			where TComponent : Component
		{
			if (comp == null) return;
			Return(comp);
		}

		public static void Return<TComponent>([NotNull] TComponent comp)
			where TComponent : Component
		{
			Return(comp.gameObject);
		}


		public static void ReturnSafe([CanBeNull] GameObject instance)
		{
			if (instance == null) return;
			Return(instance);
		}

		/// <summary>
		/// Free an instance so it can be re-used.
		/// </summary>
		public static void Return([NotNull] GameObject instance)
		{
			GObjectPool pool = Live._poolByInstance[instance];

			Live._poolByInstance.Remove(instance);
			pool.ReturnGO(instance);

			instance.transform.SetParent(Live.transform);
		}

		public static void DestroyOrReturn<TComponent>([NotNull] TComponent comp)
			where TComponent : Component
		{
			DestroyOrReturn(comp.gameObject);
		}

		public static void DestroyOrReturn([NotNull] GameObject obj)
		{
			if (Live._poolByInstance.TryGetValue(obj, out GObjectPool pool))
			{
				Live._poolByInstance.Remove(obj);
				pool.ReturnGO(obj);
				obj.transform.SetParent(Live.transform);
			}
			else
			{
				Destroy(obj);
			}
		}

		/// <summary>
		/// Prime a prefab with instances ready for use.
		/// </summary>
		public static void Prime([NotNull] GameObject prefab, int initialAllocation = -1, int maxAllocation = -1)
		{
			if (!Live._poolByPrefab.TryGetValue(prefab, out GObjectPool pool))
			{
				Live._poolByPrefab.Add(prefab, pool = new GObjectPool(Live.transform, prefab)
				{
					allocateTemp = true,
					safetyChecks = false
				});
			}

			int count = pool.CurrentSize - initialAllocation;
			if (count <= 0)
				// Already primed with as many instances as 'initialAllocation'.
				return;

			pool.maxSize = Mathf.Max(initialAllocation, maxAllocation);
			pool.AllocateAdd(count);
		}

		/// <summary>
		/// Wipe all instances of a prefab from the pool.
		/// </summary>
		public static void Wipe(GameObject prefab)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Prime a prefab with instances ready for use.
		/// </summary>
		public static void Prime(string address, int initialAllocation = -1, int maxAllocation = -1)
		{
			throw new NotImplementedException();
		}

		public static async UniTask<TComponent> RentAsync<TComponent>(string address)
			where TComponent : Component
		{
			var obj = await RentAsync(address);
			return obj.GetComponent<TComponent>();
		}

		/// <summary>
		/// Get an instance from the pool.
		/// </summary>
		public static async UniTask<GameObject> RentAsync([NotNull] string address)
		{
			if (!Live._poolByAddress.TryGetValue(address, out GObjectPool pool))
			{
				AsyncOperationHandle<GameObject> handle = await Addressables2.LoadHandleAsync<GameObject>(address);
				Live._poolByAddress.Add(address, pool = new GObjectPool(Live.transform, handle.Result)
				{
					maxSize      = 512,
					allocateTemp = true,
					safetyChecks = false
				});
				Live._handles.Add(address, handle);
			}

			GameObject instance = pool.Rent();
			Live._poolByInstance[instance] = pool;

			return instance;
		}

		/// <summary>
		/// Wipe all instances of a prefab from the pool. (by address)
		/// </summary>
		public static void Wipe(string address)
		{
			throw new NotImplementedException();
		}

		[Serializable]
		private struct PrimingEntry
		{
			[AssetsOnly]
			[Required]
			[HideLabel]
			[HorizontalGroup] public GameObject Prefab;

			[MinValue(1)]
			[HideLabel]
			[HorizontalGroup] public int Count;
		}
	}
}