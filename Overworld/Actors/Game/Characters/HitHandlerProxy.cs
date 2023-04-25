using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.Actors {
	public abstract class HitHandlerProxy<TInfo> : SerializedMonoBehaviour, IHitHandler<TInfo> where TInfo:IHitInfo {

		public List<GameObject> Targets = new List<GameObject>();

		[ShowInPlay]
		private List<IHitHandler<TInfo>> _handlers;

		private void Awake()
		{
			_handlers = new List<IHitHandler<TInfo>>();
			foreach (GameObject go in Targets)
				_handlers.AddRange(go.GetComponents<IHitHandler<TInfo>>());
		}

		public void OnHit(TInfo hit) {

			// Note(C.L. 6-19-22): Probably not a big deal as it's triggered and not likely to be intensive
			foreach (IHitHandler<TInfo> handler in _handlers)
				handler.OnHit(hit);
		}

		public bool IsHittable(TInfo hit)
		{
			foreach (IHitHandler<TInfo> handler in _handlers)
				if (!handler.IsHittable(hit)) return false;

			return true;
		}
	}
}