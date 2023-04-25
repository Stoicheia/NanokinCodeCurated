using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Util
{
	/// <summary>
	/// TODO rename to ObjectPool or GameObjectPool after we remove the old pooling shit
	/// </summary>
	public class GObjectPool : BasePool<GameObject>
	{
		public GameObject prefab;

		public GObjectPool(Transform root) : base(root) { }

		public GObjectPool(Transform root, GameObject prefab) : base(root)
		{
			this.prefab = prefab;
		}

		protected override string GetName() => prefab.name;

		protected override void SetName(GameObject poolee, string name)
		{
			poolee.name = name;
		}

		protected override GameObject CreateNew(Action<GameObject> createHandler)
		{
			GameObject ret = prefab == null ? new GameObject() : Object.Instantiate(prefab);
			createHandler?.Invoke(ret);
			return ret;
		}

		protected override void Deactivate(GameObject poolee)
		{
			poolee.transform.SetParent(root, false);
			poolee.SetActive(false);
			Recycle(poolee);
		}

		protected override void Deallocate(GameObject poolee)
		{
			Object.Destroy(poolee);
		}

		protected override void Activate(GameObject poolee)
		{
			poolee.SetActive(true);
		}

		public TMono Rent<TMono>()
		{
			return Rent().GetComponent<TMono>();
		}

		public bool ReturnGO(GameObject obj)
		{
			int id = obj.GetInstanceID();
			for (var i = 0; i < actives.Count; i++)
			{
				if (actives[i].GetInstanceID() == id)
				{
					ReturnAt(i);
					return true;
				}
			}

			return false;
		}

		public override bool ReturnSafe(GameObject obj)
		{
			int id = obj.GetInstanceID();
			for (var i = 0; i < actives.Count; i++)
			{
				if (actives[i].GetInstanceID() == id)
				{
					ReturnAt(i);
					return true;
				}
			}

			return false;
		}

		public void Return<TMono>(TMono mono)
			where TMono : Component
		{
			ReturnGO(mono.gameObject);
		}

		//too bad
		public void ActivateAll()
		{
			foreach(var ina in inactives) ina.SetActive(true);
			foreach(var act in actives) act.SetActive(true);
		}
	}
}