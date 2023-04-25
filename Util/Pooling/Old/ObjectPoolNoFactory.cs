using System;
using System.Collections.Generic;

namespace Util
{
	public class ObjectPoolNoFactory<T> : IObjectPool<T> where T:new()
	{
		private readonly Queue<T>   _queue;
		private readonly HashSet<T> _usedObjects;
		private readonly HashSet<T> _freeObjects;

		private int _allocationCount;

		public ObjectPoolNoFactory(int maxAllocationCount = 30, int initialAllocationCount = 0)
		{
			_queue       = new Queue<T>();
			_freeObjects = new HashSet<T>();
			_usedObjects = new HashSet<T>();

			MaxAllocationCount = maxAllocationCount;

			while (_allocationCount < initialAllocationCount)
				Allocate();
		}

		public int MaxAllocationCount { get; set; }

		private T TryAllocate()
		{
			bool canAllocate = _allocationCount < MaxAllocationCount;
			if (!canAllocate)
			{
				throw new Exception($"The ObjectPool is maxed out. (max={MaxAllocationCount}). Increase the limit or investigate why the limit was reached and whether a solution exist to the problem.");
			}

			return Allocate();
		}

		private T Allocate()
		{
			T allocatedObject = new T();

			_freeObjects.Add(allocatedObject);
			_allocationCount++;

			_queue.Enqueue(allocatedObject);

			return allocatedObject;
		}

		public T Get()
		{
			if (_queue.Count == 0)
			{
				TryAllocate();
			}

			return _queue.Dequeue();
		}

		public T GetAndLock()
		{
			T next = Get();
			LockPoolee(next);
			return next;
		}

		public void LockPoolee(T obj)
		{
			bool wasFree = _freeObjects.Remove(obj);

			if (!wasFree)
			{
				throw new InvalidOperationException("The object to use is not free or contained by the pool.");
			}

			_usedObjects.Add(obj);
		}

		public void ReleasePoolee(T obj)
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