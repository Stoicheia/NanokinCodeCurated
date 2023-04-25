using System.Collections.Generic;
using Anjin.Nanokin.ParkAI;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace Anjin.Regions {
	public enum POIType
	{ Food, Entertainment }

	public class ParkAIPointOfInterest : RegionMetadata
										 #if UNITY_EDITOR
										 , IRegionMetadataDrawsInEditor
		#endif
	{
		[Title("Base")]
		[EnumToggleButtons]
		public POIType Type = POIType.Entertainment;
		public int AttractionDistance = 1;


		public List<Vector3> UsageSlots = new List<Vector3>();

		[Space]
		[Title("Focus Point")]
		public bool HasFocusPoint = false;
		public Vector3 FocusPointOffset = Vector3.zero;

		[Space]
		[Title("Queue")]
		public ParkAIQueue Queue;

		#if UNITY_EDITOR

		public bool UseObjMatrix => false;

		public void SceneGUI(RegionObject obj, bool selected)
		{
			if (!(obj is RegionShape2D shape)) return;

			if(HasFocusPoint) {
				Handles.color = Color.HSVToRGB(0.6f, 1, 1);
				Handles.DrawWireCube(shape.Transform.Position + FocusPointOffset, Vector3.one * 0.5f);
			}

			if(selected && UsageSlots != null) {
				var prev = Handles.matrix;
				Handles.matrix = shape.Transform.matrix;
				for (int i = 0; i < UsageSlots.Count; i++) {
					UsageSlots[i] = Handles.PositionHandle(UsageSlots[i], Quaternion.identity);
				}

				Handles.matrix = prev;
			}

			Queue?.DrawHandles(shape.Transform.matrix, selected);
		}

		#endif

	}
}