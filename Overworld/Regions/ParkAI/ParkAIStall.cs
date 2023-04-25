using System.Collections.Generic;
using Anjin.Nanokin.ParkAI;
using Drawing;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;

#if UNITY_EDITOR
using UnityEditor;
using Sirenix.Utilities.Editor;
#endif

namespace Anjin.Regions {

	// Stall: Anything a peep can queue to interacts with. (Doesn't have to be an actual shop)
	//----------------------------------------------------------------------------------------

	public enum StallType : byte {
		Food,
		Game,
		Souvenir,

	}

	public struct UsageSlot {
		public Vector3 position;
		public float   direction;
	}

	public class ParkAIStall : RegionMetadata
	 #if UNITY_EDITOR
	 , IRegionMetadataDrawsInEditor
	#endif
	{
		[Title("Base")]
		[EnumToggleButtons]
		public StallType Type = StallType.Food;
		public int AttractionDistance = 1;

		public List<UsageSlot> UsageSlots = new List<UsageSlot>();

		public StallBrain Brain;

		[Space]
		[Title("Queue")]
		public ParkAIQueue Queue;

		#if UNITY_EDITOR

		public bool UseObjMatrix => false;

		public void SceneGUI(RegionObject obj, bool selected)
		{
			if (!(obj is RegionShape2D shape)) return;

			if(UsageSlots != null) {
				var prev = Handles.matrix;
				Handles.matrix = shape.Transform.matrix;
				for (int i = 0; i < UsageSlots.Count; i++) {
					UsageSlot slot = UsageSlots[i];
					if(selected) {
						slot.position = Handles.PositionHandle(slot.position, Quaternion.identity);
					}
					UsageSlots[i]  = slot;

					if (Event.current.OnRepaint()) {
						Vector3 point = shape.Transform.TransformPoint(slot.position);
						Draw.Circle(point,Vector3.up, 0.3f, ColorsXNA.LavenderBlush);
						Draw.ArrowheadArc(point, MathUtil.DegreeToVector3XZ(slot.direction), 0.3f, 50f, ColorsXNA.LavenderBlush);
					}
				}

				Handles.matrix = prev;
			}

			Queue?.DrawHandles(shape.Transform.matrix, selected);
		}

		#endif

	}
}