using System;
using JetBrains.Annotations;
using Overworld.Controllers;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Util
{
	// public  Entry<TMono>
	// 	where TMono : Behaviour
	// {
	// 	public TMono             mono;
	// 	public int               index;
	// 	public List<IRecyclable> recyclables;
	// }

	public class ComponentPool<TMono> : BasePool<TMono>
		where TMono : Behaviour
	{
		public TMono prefab;
		public bool  overrideTags;


		public ComponentPool(Transform root) : base(root) { }

		public ComponentPool(Transform root, TMono prefab) : base(root)
		{
			this.prefab = prefab;
		}

		public override void Destroy()
		{
			for (var i = 0; i < inactives.Count; i++)
			{
				Object.Destroy(inactives[i].gameObject);
			}

			for (var i = 0; i < actives.Count; i++)
			{
				Object.Destroy(actives[i].gameObject);
			}
		}

		protected override void Deactivate([NotNull] TMono poolee)
		{
			poolee.transform.SetParent(root, true);

			Enabler.Set(poolee.gameObject, false, SystemID.Pool);
			Recycle(poolee.gameObject);
		}

		protected override void Deallocate([NotNull] TMono poolee)
		{
			Object.Destroy(poolee.gameObject);
		}

		[NotNull] protected override string GetName() => prefab == null ? typeof(TMono).Name : prefab.name;

		protected override void SetName([NotNull] TMono poolee, [NotNull] string name)
		{
			poolee.name = name;
		}

		protected override void Activate([NotNull] TMono poolee)
		{
			Enabler.Set(poolee.gameObject, true, SystemID.Pool);
		}

		[NotNull]
		protected override TMono CreateNew([CanBeNull] Action<TMono> createHandler)
		{
			TMono ret;
			if (prefab == null)
			{
				var go = new GameObject();
				ret = go.AddComponent<TMono>();
			}
			else
			{
				ret = Object.Instantiate(prefab);
			}

			ret.transform.SetParent(root, false);

			createHandler?.Invoke(ret);
			return ret;
		}

		public override void Return([NotNull] TMono obj)
		{
			int id = obj.GetInstanceID();
			for (var i = 0; i < actives.Count; i++)
			{
				if (actives[i].GetInstanceID() == id)
				{
					ReturnAt(i);
					return;
				}
			}
		}

		/// <summary>
		/// Return the object. If it's null, do nothing and returns false.
		/// If it can be guaranteed that the object won't be null, use Return
		/// instead for better performances. (especially in a performance critical context)
		/// </summary>
		/// <param name="obj"></param>
		/// <returns>Returns true if the object was properly released.</returns>
		public override bool ReturnSafe(TMono obj)
		{
			if (obj == null)
				return false;

			int id = obj.GetInstanceID();
			for (var i = 0; i < actives.Count; i++)
			{
				if (actives[i].GetInstanceID() == id)
					return ReturnAt(i);
			}

			return false;
		}
	}
}