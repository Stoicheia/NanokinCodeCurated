using System.Collections.Generic;
using Drawing;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Util.Extensions;

namespace Anjin.Regions {

	//	Rest Points
	//============================================================================
	public enum RestPointType { Sitting = 0, }

	public struct RestPoint
	{
		[EnumToggleButtons]
		public RestPointType type;
		public Vector3 offset;
		public float   facing_degrees;
		public bool    has_mount;
		public Vector3 mount_offset;

		public Vector3 GetFacingVector() => ( Quaternion.AngleAxis(facing_degrees, Vector3.up) * Vector3.forward ).normalized;
	}

	public class ParkAIRestingPoint : RegionMetadata, IRegionMetadataDrawsInEditor
	{
		public List<RestPoint> spaces = new List<RestPoint>();
		#if UNITY_EDITOR

		public bool UseObjMatrix => true;

		public void SceneGUI(RegionObject obj, bool selected)
		{
			if (!(obj is RegionObjectSpatial spatial)) return;

			for (int i = 0; i < spaces.Count; i++) {
				var space = spaces[i];
				DrawHandle(ref space, spatial.Transform.matrix, selected);
				spaces[i] = space;
			}
		}

		public static void DrawHandle(ref RestPoint point, Matrix4x4 mat, bool editable)
		{
			Vector3 pos = mat.MultiplyPoint3x4(point.offset);

			Draw.WireBox(pos, Vector3.one * 0.35f, ColorsXNA.SeaGreen);
			Draw.Line(pos, pos +  mat.rotation * point.GetFacingVector() * 0.5f, ColorsXNA.Orange);

			if(point.has_mount) {
				Draw.WireBox(pos + mat.rotation * point.mount_offset, Vector3.one * 0.15f, Color.HSVToRGB(0.4f, 1, 1));
			}

			if(editable) {
				point.offset = Handles.DoPositionHandle(point.offset, Quaternion.identity);
			}
		}

		#endif
	}


}