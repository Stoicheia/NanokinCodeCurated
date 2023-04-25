using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Util.ObjectProviders
{
	public abstract class ObjectProvider : SerializedMonoBehaviour, IEnumerable<GameObject>
	{
		public abstract bool HasRoom { get; }

		protected abstract IEnumerable<GameObject> AllObjects { get; }

		public abstract GameObject GetNext();

		public abstract GameObject Get(int index);

		public IEnumerator<GameObject> GetEnumerator()
		{
			return AllObjects.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}