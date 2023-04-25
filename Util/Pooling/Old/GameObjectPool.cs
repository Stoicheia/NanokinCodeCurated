using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Util
{
	public class GameObjectPool : SerializedMonoBehaviour, IObjectFactory<GameObject>
	{
		[FormerlySerializedAs("_prefab"),SerializeField, Required]
		public GameObject Prefab;
		[FormerlySerializedAs("_parentToPoolByDefault"),SerializeField]                                                 public bool ParentToPoolByDefault = true;
		[FormerlySerializedAs("_maxAllocationCount"),SerializeField, OnValueChanged("OnMaxAllocationCountChanged")]     public int  MaxAllocationCount    = 30;
		[FormerlySerializedAs("_initialAllocationCount"),SerializeField, OnValueChanged("OnMaxAllocationCountChanged")] public int  InitialAllocationCount;

		protected ObjectPool<GameObject> pool;

		private void Awake()
		{
			pool = new ObjectPool<GameObject>(this, MaxAllocationCount, InitialAllocationCount);
		}


		public GameObject GetAndLock()
		{
			GameObject next = pool.Get();
			LockPoolee(next);

			return next;
		}

		public GameObject Get()
		{
			return pool.Get();
		}

		public void LockPoolee(GameObject obj)
		{
			if (obj == null) return;
			pool.LockPoolee(obj);
			obj.SetActive(true);
		}

		public void ReleasePoolee(GameObject obj)
		{
			if (obj == null) return;
			obj.SetActive(false);
			pool.ReleasePoolee(obj);
		}

		public GameObject BuildObject()
		{
			GameObject go = Instantiate(Prefab);
			go.SetActive(false);

			if (ParentToPoolByDefault)
			{
				go.transform.SetParent(transform, false);
			}

			return go;
		}

		/// <summary>
		/// Get and use a gameObject from the pool, then gets and returns the specified component on it.
		/// </summary>
		/// <param name="pool"></param>
		/// <typeparam name="TComponent"></typeparam>
		/// <returns></returns>
		public TComponent GetAndLock<TComponent>()
			where TComponent : Component
		{
			return GetAndLock().GetComponent<TComponent>();
		}

		/// <summary>
		/// Free the gameObject owning the component.
		/// </summary>
		/// <param name="pool"></param>
		/// <param name="component"></param>
		/// <typeparam name="TComponent"></typeparam>
		public void ReleasePoolee<TComponent>(TComponent component)
			where TComponent : Component
		{
			ReleasePoolee(component.gameObject);
		}

#if UNITY_EDITOR
		public void OnMaxAllocationCountChanged()
		{
			if (pool != null)
				pool.MaxAllocationCount = MaxAllocationCount;
		}

		[Button, Title("Debugging")]
		private void Reset()
		{
			pool = new ObjectPool<GameObject>(this, MaxAllocationCount, InitialAllocationCount);
		}

		[Button]
		private void Spawn()
		{
			Instantiate(Prefab, transform);
		}
#endif
	}
}