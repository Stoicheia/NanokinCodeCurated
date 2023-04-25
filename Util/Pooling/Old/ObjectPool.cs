using System;
using System.Collections.Generic;

namespace Util
{
	public class ObjectPool<TObject> : IObjectPool<TObject>
	{
		private readonly IObjectFactory<TObject> _objectFactory;

		private readonly Queue<TObject>   _queue;
		private readonly HashSet<TObject> _usedObjects;
		private readonly HashSet<TObject> _freeObjects;

		private int _allocationCount;

		public ObjectPool(IObjectFactory<TObject> objectFactory, int maxAllocationCount = 30, int initialAllocationCount = 0)
		{
			_objectFactory = objectFactory;

			_queue       = new Queue<TObject>();
			_freeObjects = new HashSet<TObject>();
			_usedObjects = new HashSet<TObject>();

			MaxAllocationCount = maxAllocationCount;

			while (_allocationCount < initialAllocationCount)
				Allocate();
		}

		public int MaxAllocationCount { get; set; }

		private TObject TryAllocate()
		{
			bool canAllocate = _allocationCount < MaxAllocationCount;
			if (!canAllocate)
			{
				throw new Exception($"The ObjectPool is maxed out. (max={MaxAllocationCount}). Increase the limit or investigate why the limit was reached and whether a solution exist to the problem.");
			}

			return Allocate();
		}

		private TObject Allocate()
		{
			TObject allocatedObject = _objectFactory.BuildObject();

			_freeObjects.Add(allocatedObject);
			_allocationCount++;

			_queue.Enqueue(allocatedObject);

			return allocatedObject;
		}

		public TObject Get()
		{
			if (_queue.Count == 0)
			{
				TryAllocate();
			}

			return _queue.Dequeue();
		}

		public TObject GetAndLock()
		{
			TObject next = Get();
			LockPoolee(next);
			return next;
		}

		public void LockPoolee(TObject obj)
		{
			bool wasFree = _freeObjects.Remove(obj);

			if (!wasFree)
			{
				throw new InvalidOperationException("The object to use is not free or contained by the pool.");
			}

			_usedObjects.Add(obj);
		}

		public void ReleasePoolee(TObject obj)
		{
			bool wasUsed = _usedObjects.Remove(obj);

			if (!wasUsed)
			{
				throw new InvalidOperationException("The object to use is not free or contained by the pool.");
			}

			_freeObjects.Add(obj);
			_queue.Enqueue(obj);
		}
	}
}