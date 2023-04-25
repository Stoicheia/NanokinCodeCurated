using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Anjin.Actors;
using Anjin.Scripting;
using Anjin.Util;
using JetBrains.Annotations;
using UnityEngine;
using Util;

namespace Combat.Data
{
	/// <summary>
	/// A proc table which holds procs for easy retrieval
	/// and common usage.
	/// </summary>
	[LuaUserdata]
	public class ProcTable : IEnumerable<Proc>
	{
		public List<Proc> all;

		public ProcTable([NotNull] List<Proc> all)
		{
			this.all = all.ToList();
		}

		[UsedImplicitly]
		public int Remaining => all.Count;

		[UsedImplicitly]
		public int Length => all.Count;

		[UsedImplicitly]
		public bool any => all.Count > 0;

		[UsedImplicitly]
		public Vector3 center
		{
			get
			{
				Centroid c = new Centroid();
				foreach (Proc proc in all)
					c.add(proc.center);
				return c.get();
			}
		}

		[CanBeNull]
		[UsedImplicitly]
		public ActorBase actor => next()?.actor;

		[CanBeNull]
		[UsedImplicitly]
		public Transform transform => next()?.transform;

		[UsedImplicitly]
		public Vector3 pos => next()?.pos ?? Vector3.zero;

		[UsedImplicitly]
		public Vector3 facing => next()?.facing ?? Vector3.zero;



		[UsedImplicitly]
		public Proc this[int index] => all.SafeGet(index);

		// Indexer by fighter
		[UsedImplicitly]
		public Proc this[Fighter fighter]
		{
			get
			{
				foreach (Proc proc in all)
					if (proc.fighters.Contains(fighter))
						return proc;
				return null;
			}
		}

		[UsedImplicitly]
		public List<Proc> Skip(int n)
		{
			// return all.Skip(n).ToList();
			// rewritten without linq
			var list = new List<Proc>();
			for (int i = n; i < all.Count; i++)
				list.Add(all[i]);
			return list;
		}

		public bool Pop(Proc proc)
		{
			int i = all.IndexOf(proc);
			if (i == -1)
			{
				Debug.LogError($"ProcTable: {proc} not found in table or already fired.");
				return false;
			}

			all.RemoveAt(i);
			return true;
		}

		public bool Pop(string id, out Proc proc)
		{
			int i = all.FindIndex(v => v.ID == id);
			if (i == -1)
			{
				Debug.LogError($"ProcTable: proc '{id}' not found in table or already fired.");

				proc = null;
				return false;
			}

			proc = all[i];
			all.RemoveAt(i);
			return true;
		}

		/// <summary>
		/// Get the next matching Proc for the id.
		/// Tries named procs first, then the any list which advances.
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public bool PopNext(out Proc proc)
		{
			if (all.Count <= 0)
			{
				proc = null;
				return false;
			}

			proc = all.First();
			all.RemoveAt(0);
			return true;
		}

		[UsedImplicitly]
		public Proc next()
		{
			if (all.Count <= 0)
				return null;

			return all.First();
		}

		public IEnumerator<Proc> GetEnumerator() => all.GetEnumerator();

		IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

		public override string ToString()
		{
			var s = "";
			foreach (Proc proc in all)
				s += $"{proc.ID}, ";
			if (s.Length > 0)
				s = s.Substring(0, s.Length - 2);
			return s;
		}
	}
}