
using System;
using System.Collections.Generic;
using Drawing;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Components;

namespace Anjin.Regions {
	public class SceneParkAIRestPoint : SceneRegionObjectBase, IShouldDrawGizmos {

		public List<RestPoint> Points = new List<RestPoint>();

		//public Transform test;

		protected override void OnRegisterDrawer() => DrawingManagerProxy.Register(this);
		private            void OnDestroy()        => DrawingManagerProxy.Deregsiter(this);

		public override bool AddObjectToList(List<RegionObject> Objects)
		{
			RegionShape2D shape = new RegionShape2D();
			ParkAIRestingPoint rest = new ParkAIRestingPoint();
			rest.spaces = Points;

			shape.Metadata.Add(rest);

			return true;
		}

		#if UNITY_EDITOR
		public override void DrawGizmos()
		{
			if (Points == null) return;


			/*if(test != null) {
				Matrix4x4 mat = test.localToWorldMatrix;

				var     col  = Color.white;
				Vector3 size =  new float3(3, 4, 2);

				if (IntersectionUtil.Intersection_PointBox(transform.localToWorldMatrix.MultiplyPoint3x4(LinkOffset), mat, size / 2))
					col = Color.red;

				using (Draw.WithMatrix(mat)) {
					Draw.WireBox(Vector3.zero, size, col);
				}

			}*/

			//using (Draw.WithMatrix(transform.localToWorldMatrix.TR())) {
				Draw.Cross(transform.localToWorldMatrix.MultiplyPoint(LinkOffset), Color.green);
			//}

			for (var i = 0; i < Points.Count; i++) {
				RestPoint p = Points[i];
				ParkAIRestingPoint.DrawHandle(ref p, transform.localToWorldMatrix, false);
			}

		}

		/*private void OnDrawGizmosSelected()
		{
			if (Points == null) return;

			Matrix4x4 prev = Handles.matrix;
			Handles.matrix = transform.localToWorldMatrix;
			for (var i = 0; i < Points.Count; i++) {
				RestPoint p = Points[i];
				p.offset  = Handles.PositionHandle(Points[i].offset, Quaternion.identity);
				Points[i] = p;
			}

			Handles.matrix = prev;
		}*/

		#endif

		[ShowInInspector] public static bool  AlwaysDraw;
		[ShowInInspector] public static float EditorViewDistance = 50f;

		public bool ShouldDrawGizmos()
		{
			if (AlwaysDraw) return true;

			Vector3    cpos   = Camera.current.transform.position;

			if (Vector3.Distance(cpos, transform.position) > EditorViewDistance) return false;

			var    ltw    = transform.localToWorldMatrix;
			Bounds bounds = new Bounds(ltw.MultiplyPoint(LinkOffset), new Vector3(0.5f, 0.5f, 0.5f));

			for (var i = 0; i < Points.Count; i++) {

				//Draw.WireBox(bounds, Color.red);
				RestPoint p = Points[i];
				bounds.Encapsulate(new Bounds(ltw.MultiplyPoint3x4(p.offset), new Vector3(0.5f, 0.5f, 0.5f)));
			}

			return GeometryUtility.TestPlanesAABB(DrawingManager.CurrentCamFrustumPlanes, bounds);

		}
	}
}