using Sirenix.OdinInspector;
using UnityEngine;
using Util.Math.Splines;

namespace Combat.Data
{
	[DefaultExecutionOrder(0)]
	public class SlotLine : LineShape
	{
		[PropertyOrder(-1)]
		public Vector2Int MatrixForward;

		[PropertyOrder(-1)]
		public string[] Tags;

		// public List<Battle.Slot> AllSlots { get; } = new List<Battle.Slot>();

		private void Awake()
		{
			// foreach (Plot plot in Plots)
			// {
			// 	var obj = new GameObject("WorldSlot (temp)");
			//
			// 	Battle.Slot slot = obj.AddComponent<Battle.Slot>();
			// 	slot.transform.position = plot.Position;
			// 	slot.forwardAngle       = ForwardAngle;
			// 	slot.matrixForward      = MatrixForward;
			//
			// 	SceneManager.MoveGameObjectToScene(obj, gameObject.scene);
			//
			// 	AllSlots.Add(slot);
			// }
		}
	}
}