using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Pathfinding.Util;
using UnityEngine;
using Util.Odin.Attributes;

namespace Util
{
	/// <summary>
	/// A simple and straightforward pool with implementations for different types of objects.
	/// - GObjectPool to pool game objects.
	/// - ComponentPool to pool components. (gameobjects as components)
	///
	/// Objects can implement IRecyclable to run some reset
	/// logic when the object is recycled into the inactives.
	/// </summary>
	/// <typeparam name="TPoolee"></typeparam>
	public abstract class BasePool<TPoolee> : IPool<TPoolee>
		where TPoolee : class
	{
		[ShowInPlay] public List<TPoolee> inactives;
		[ShowInPlay] public List<TPoolee> actives;

		/// <summary>
		/// Maximum size of the pool.
		///
		/// The pool can contain more, but it will not allocate past this number.
		/// Set to -1 for unlimited size.
		/// </summary>
		public int maxSize = 0;

		/// <summary>
		/// A transform that all objects should be parented to.
		/// The parent can be changed while the object is in use, but
		/// it will be reparented to this root when it is returned.
		/// </summary>
		public Transform root;

		/// <summary>
		/// Name of the game objects that will be allocated.
		/// </summary>
		public string objectName = "Pool Allocation";

		/// <summary>
		/// Allocate temporary objects if the pool runs out.
		/// A safety feature that can be enabled to try and
		/// keep the game more stable during development, where
		/// bugs can lead a pool to empty out in milliseconds.
		/// </summary>
		public bool
			allocateTemp = false; // TODO remove this field and make this a built-in feature always true. Returning null almost always breaks the game anyway. Instead, we should auto-pause the editor when something suspicious happens to avoid allocating like crazy.

		/// <summary>
		/// Checks that an object is not already in the inactive list
		/// before returning it.
		/// </summary>
		public bool safetyChecks = true;

		public bool throwsOnCantAllocate = true;

		/// <summary>
		/// Allocate N values, but only if the pool is currently empty.
		///
		/// A syntax sugar for combining with other fields above
		/// in an object constructor.
		/// </summary>
		public int initSize
		{
			set
			{
				if (CurrentSize == 0)
				{
					AllocateAdd(value);
				}
			}
		}

		public Action<TPoolee> onAllocating;

		private HashSet<TPoolee> _temporaryAllocations = new HashSet<TPoolee>();

		public int CurrentSize => inactives.Count + actives.Count;

		public int ActiveCount => actives.Count;

		/// <summary>
		/// Instantiate a base pool that will be filled manually.
		/// </summary>
		/// <param name="root"></param>
		protected BasePool(Transform root)
		{
			this.root = root;
			inactives = new List<TPoolee>();
			actives   = new List<TPoolee>();
		}

		/// <summary>
		/// Allocate N objects and adds them to the inactives.
		/// </summary>
		public void AllocateAdd(int n)
		{
			for (int i = 0; i < n; i++)
				AllocateAdd();
		}

		/// <summary>
		/// Allocate a new object and add it to the inactives.
		/// </summary>
		public TPoolee AllocateAdd()
		{
			TPoolee ret = CreateNew(onAllocating);
			SetName(ret, GetNameForPoolee());
			AddInactive(ret);
			return ret;
		}

		/// <summary>
		/// Add an object to the inactives.
		/// </summary>
		/// <param name="obj"></param>
		public void AddInactive([NotNull] TPoolee obj)
		{
			inactives.Add(obj);
			Deactivate(obj);
		}

		/// <summary>
		/// Rent an inactive object for use.
		/// It must be returned later!
		/// </summary>
		[CanBeNull]
		public TPoolee Rent()
		{
			TPoolee obj = null;
			if (inactives == null)
			{
				inactives = new List<TPoolee>();
			}

			if (actives == null)
			{
				actives = new List<TPoolee>();
			}

			if (inactives.Count != 0)
			{
				// Get existing
				// ----------------------------------------
				obj = inactives[inactives.Count - 1];
				inactives.RemoveAt(inactives.Count - 1);
			}
			else
			{
				if (maxSize > -1 && CurrentSize >= maxSize)
				{
					// Temporary allocation
					// ----------------------------------------
					if (CurrentSize >= maxSize && !allocateTemp) {
						if (throwsOnCantAllocate)
							throw new Exception("BasePool: Cannot rent an instance from the pool because it is maxed out and we can't allocate temporaries.");
						else
							return null;
					}

					obj = CreateNew(onAllocating);
					SetName(obj, GetNameForPoolee("TEMP"));

					_temporaryAllocations.Add(obj);
				}
				else
				{
					// Allocate a new one
					// ----------------------------------------
					obj = CreateNew(onAllocating);
					SetName(obj, GetNameForPoolee());
				}
			}

			actives.Add(obj);
			Activate(obj);

			return obj;
		}

		/// <summary>
		/// Rent an object for use.
		/// </summary>
		public (List<TPoolee>, bool) Rent(int number)
		{
			List<TPoolee> list = null;

			// Rent() already handles allocation,
			// this should be just a simple wrapper

			// if (inactives.Count == 0 || inactives.Count < number) {
			//
			// 	if(number + inactives.Count > maxSize)
			// 		return (null, false);
			//
			// 	AllocateAdd(number - inactives.Count);
			// }
			//
			// List<TPoolee> list = new List<TPoolee>();

			for (int i = 0; i < number; i++)
			{
				TPoolee obj = Rent();

				list = list ?? new List<TPoolee>();
				list.Add(obj);
			}

			return (list, true);
		}

		/// <summary>
		/// Return an object which is currently active.
		/// </summary>
		public virtual bool ReturnAt(int index)
		{
			TPoolee obj = actives[index];

			if (_temporaryAllocations.Contains(obj))
			{
				Deallocate(obj);
				actives.RemoveAt(index);
				return true;
			}

			if (!safetyChecks || !inactives.Contains(obj))
			{
				inactives.Add(actives[index]);
			}
			else
			{
				DebugLogger.LogError($"BasePool: Could not return ", LogContext.Data, LogPriority.Low);
			}

			actives.RemoveAt(index);
			Deactivate(obj);

			return true;
		}

		/// <summary>
		/// Return an active object to the inactive list.
		/// </summary>
		public virtual void Return(TPoolee obj)
		{
			int index = actives.IndexOf(obj);
			if (index == -1)
				throw new ArgumentException();

			ReturnAt(index);
		}

		/// <summary>
		/// Return an active object to the inactive list.
		/// </summary>
		///
		public virtual bool ReturnSafe(TPoolee obj)
		{
			if (obj == null) return false;
			int index = actives.IndexOf(obj);
			if (index == -1)
				return false;

			return ReturnAt(index);
		}

		/// <summary>
		/// Return all active instances.
		/// </summary>
		public void ReturnAll()
		{
			int c = actives.Count;
			for (int i = 0; i < c; i++)
			{
				ReturnAt(0);
			}

			actives.Clear();
		}

		public virtual void Destroy() { }

		protected void Recycle(object o)
		{
			if (o is IRecyclable recyclable)
			{
				recyclable.Recycle();
			}
		}

		protected void Recycle(GameObject go)
		{
			List<IRecyclable> scratch = ListPool<IRecyclable>.Claim();

			go.GetComponentsInChildren(scratch);
			for (int i = 0; i < scratch.Count; i++)
			{
				IRecyclable recyclable = scratch[i];
				recyclable.Recycle();
			}

			ListPool<IRecyclable>.Release(scratch);
		}

		private string GetNameForPoolee(string id = null)
		{
			return $"{GetName() ?? objectName} [{id ?? (actives.Count + inactives.Count).ToString()}]";
		}

		protected abstract TPoolee CreateNew(Action<TPoolee> createHandler);
		protected abstract string  GetName();
		protected abstract void    SetName(TPoolee    poolee, string name);
		protected abstract void    Activate(TPoolee   poolee);
		protected abstract void    Deactivate(TPoolee poolee);
		protected abstract void    Deallocate(TPoolee poolee);

		public override string ToString()
		{
			return $"{GetType().Name}(inactives: {inactives.Count}, actives: {actives.Count}, maxSize: {maxSize})";
		}
	}
}