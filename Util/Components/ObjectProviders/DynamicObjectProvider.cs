using System.Collections.Generic;
using Anjin.Util;
using UnityEngine;

namespace Util.ObjectProviders
{
	public class DynamicObjectProvider : ObjectProvider
	{
		[SerializeField] private GameObject _prefab;
		[SerializeField] private bool       _parentToProviderByDefault = true;

		private List<GameObject> _all = new List<GameObject>();

		public override    bool                    HasRoom    => true;
		protected override IEnumerable<GameObject> AllObjects => _all;

		public override GameObject GetNext()
		{
			GameObject go = _prefab.InstantiateNew();
			go.SetActive(true);

			if (_parentToProviderByDefault)
				go.transform.parent = transform;

			_all.Add(go);
			return go;
		}

		public override GameObject Get(int index)
		{
			return _all[index];
		}
	}
}