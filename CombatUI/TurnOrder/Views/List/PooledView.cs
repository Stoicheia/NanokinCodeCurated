using UnityEngine;

namespace Combat.UI.TurnOrder
{
	/// <summary>
	/// A pooled action view in the ViewPool.
	/// </summary>
	public class PooledView
	{
		public readonly GameObject prefab;
		public readonly GameObject gameObject;
		public readonly ViewTurn   vc;

		public PooledView(GameObject prefab, Transform parent)
		{
			this.prefab = prefab;

			gameObject = Object.Instantiate(prefab, parent);
			vc       = gameObject.GetComponent<ViewTurn>();

			gameObject.SetActive(false);
		}
	}
}