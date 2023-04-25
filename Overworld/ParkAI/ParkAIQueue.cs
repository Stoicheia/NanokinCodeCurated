using System.Collections.Generic;
using Drawing;
using JetBrains.Annotations;
using UnityEngine;

#if UNITY_EDITOR
using Sirenix.Utilities.Editor;
using UnityEditor;
#endif

namespace Anjin.Nanokin.ParkAI {

	public class ParkAIQueue {

		public const float SLOT_RADIUS = 0.4f;

		public List<Vector3> Points = new List<Vector3>();

		[Min(0.2f)]
		public float SlotSeperation = 0.8f;

		public bool Valid => Points != null && Points.Count > 1;

		public float GetTotalLength()
		{
			if (!Valid) return 0;
			float length = 0;

			for (int i = 0; i < Points.Count - 1; i++)
				length += Vector3.Distance(Points[i], Points[i + 1]);

			return length;
		}

		public bool GetNumSlots(out int number)
		{
			number = 0;
			if (!Valid) return false;
			number = Mathf.FloorToInt(GetTotalLength() / SlotSeperation);
			return true;
		}

		public bool GetSlotAt(int index, out Vector3 slot, out Vector3 direction)
		{
			slot 		= Vector3.zero;
			direction 	= Vector3.forward;

			if (!Valid) return false;
			float length   = GetTotalLength();
			int   numSlots = Mathf.FloorToInt(length / SlotSeperation);
			if (numSlots == 0 || index > numSlots) return false;

			float slotDistance = SlotSeperation * index;

			float distance = 0;
			for (int i = 0; i < Points.Count - 1; i++) {
				float distBetween = Vector3.Distance(Points[i], Points[i + 1]);
				if (distance + distBetween > slotDistance) {
					slot = Points[i] + (Points[i] - Points[i + 1]).normalized * (distance - slotDistance);

					if(Vector3.Distance(Points[i], slot) > SLOT_RADIUS * 0.75f || i == 0)
						direction = (Points[i] - Points[i + 1]).normalized;
					else {
						direction = (Points[i - 1] - Points[i]).normalized;
					}

					break;
				}
				distance += distBetween;
			}

			return true;
		}

		[CanBeNull]
		public Vector3[] GetAllSlotPositions()
		{
			if (!Valid) return null;
			float length   = GetTotalLength();
			int   numSlots = Mathf.FloorToInt(length / SlotSeperation);
			if (numSlots == 0) return null;

			Vector3[] array = new Vector3[numSlots];

			for (int i = 0; i < numSlots; i++) {
				if (GetSlotAt(i, out Vector3 slot, out Vector3 direction))
					array[i] = slot;
				else
					break;
			}

			return array;
		}

		#if UNITY_EDITOR
		public void DrawHandles(Matrix4x4 parentTransform, bool editable)
		{
			if (!Valid) return;
			Matrix4x4 prev = Handles.matrix;
			Handles.matrix = parentTransform;

			bool repaint = Event.current.OnRepaint();
			using (Draw.WithMatrix(parentTransform)) {
				if(repaint)
					Draw.Polyline(Points);

				for (int i = 0; i < Points.Count; i++) {
					if(repaint) {
						Draw.CircleXZ(Points[i], 0.075f, ColorsXNA.BlueViolet);
						Draw.Label2D(Points[i] + Vector3.up * 0.25f, i.ToString(), Color.yellow);
					}

					if (editable)
						Points[i] = Handles.DoPositionHandle(Points[i], Quaternion.identity);
				}

				if (repaint && GetNumSlots(out int num)) {
					for (int i = 0; i < num; i++) {
						if (GetSlotAt(i, out Vector3 slot, out Vector3 direction))
							Draw.CircleXZ(slot, SLOT_RADIUS, ColorsXNA.OrangeRed);
							Draw.ArrowheadArc(slot, direction, SLOT_RADIUS, ColorsXNA.OrangeRed);
					}

				}
			}

			Handles.matrix = prev;
		}
		#endif

	}
}