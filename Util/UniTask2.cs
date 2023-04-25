using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cysharp.Threading.Tasks;
using Pathfinding.Util;

public static class UniTask2
{
	public static UniTaskBatch Batch()
	{
		UniTaskBatch batch = ObjectPoolSimple<UniTaskBatch>.Claim();
		batch.pooled = true;
		return new UniTaskBatch(8);
	}

	public static void ReleaseBatch(UniTaskBatch batch)
	{
		ObjectPoolSimple<UniTaskBatch>.Release(ref batch);
	}

	public static UniTask Batch(this UniTask t, UniTaskBatch batch)
	{
		batch.tasks.Add(t);
		return t;
	}

	public static UniTask<T> Batch<T>(this UniTask<T> t, UniTaskBatch batch)
	{
		batch.tasks.Add(t);
		return t;
	}

	// Note(C.L. 8-9-22): MOVED TO GAMECONTROLLER
	private static void InitPlayerLoopHelper() { }

	public static UniTask Seconds(float seconds) => UniTask.Delay(TimeSpan.FromSeconds(seconds));
	public static UniTask Frames(int    frames)  => UniTask.DelayFrame(frames);
}

/// <summary>
/// A utility for batching UniTasks (replacement for UniTask.WhenAll)
/// It can be used on its own, or through renting a pooled instance with
/// UniTask2.Batch() and UniTask2.ReleaseBatch(...)
/// The batch is cleared automatically after awaiting it.
///
/// e.g.:
///
/// batch.add(a)
/// batch.add(b)
/// batch.add(c)
/// await batch
///
/// batch.add(d)
/// batch.add(e)
/// await batch
///
/// </summary>
public class UniTaskBatch : IDisposable
{
	public List<UniTask> tasks;
	public bool          pooled = false;

	public UniTaskBatch()
	{
		tasks = new List<UniTask>(8);
	}

	public UniTaskBatch(int capacity)
	{
		tasks = new List<UniTask>(capacity);
	}

	public bool IsWaitNecessary
	{
		get
		{
			// Check if we have any task at all
			if (tasks.Count == 0) return false;

			// Check if any of them is not pending
			for (int i = 0; i < tasks.Count; i++)
			{
				if (tasks[i].Status == UniTaskStatus.Pending)
					return true;
			}

			return false;
		}
	}

	[DebuggerHidden]
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public UniTask.Awaiter GetAwaiter()
	{
		return AsTask().GetAwaiter();
	}

	public async UniTask AsTask()
	{
		await UniTask.WhenAll(tasks);
		tasks.Clear();
	}

	public void Dispose()
	{
		if (tasks != null)
		{
			tasks.Clear();
			if (pooled)
				UniTask2.ReleaseBatch(this);
		}
	}

	public void Add(UniTask task)
	{
		tasks.Add(task);
	}
}