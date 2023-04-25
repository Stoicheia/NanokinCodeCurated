using System;
using Anjin.Cameras;
using Drawing;
using KinematicCharacterController;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using Util.Components;

namespace Anjin.Nanokin.Park {
	public class EncounterBounds : AnjinBehaviour {
		public enum Shape {
			Circle,
			Rect
		}

		public bool Enabled = true;

		public Shape shape;

		public float   Radius;
		public Vector2 Size;		// Extents in either direction
		public Vector3 Offset;

		public Vector3   ShapeCenter => Offset;
		public Matrix4x4 ShapeMatrix => Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);

		public override void DrawGizmos()
		{
			if (!Enabled) return;

			using(Draw.WithMatrix(ShapeMatrix)) {
				switch (shape) {
					case Shape.Circle:
						Draw.CircleXZ(ShapeCenter, Radius, ColorsXNA.LimeGreen);
						break;

					case Shape.Rect:
						Draw.WireRectangleXZ(ShapeCenter, Size * 2, ColorsXNA.LimeGreen);
						break;
				}
			}

			/*var view = SceneView.currentDrawingSceneView;
			if(view) {
				var mpos = Event.current.mousePosition;
				mpos.y = view.position.height - mpos.y;
				var ray = view.camera.ScreenPointToRay(mpos);

				if (Physics.Raycast(ray, out var hit, 500, LayerMask.GetMask("Walkable"))) {
					var p = LimitWorldPoint(hit.point);
					Draw.WireSphere(hit.point, 0.1f, IsPointInBounds(hit.point) ? Color.blue : Color.red);
					Draw.WireSphere(p,         0.1f, Color.green);

					/*if(IsPointInBounds(hit.point)) {
						Draw.WireSphere(hit.point, 0.1f, Color.red);
					}#1#
				}
			}*/

		}

		public bool IsPointInBounds(Vector3 worldPos)
		{
			//var shape_xz = transform.position.xz();
			var xz       = ShapeMatrix.inverse.MultiplyPoint3x4(worldPos).xz();

			switch (shape) {
				case Shape.Circle:
					return xz.magnitude < Radius;
					break;

				case Shape.Rect:
					return (xz.x < Size.x && xz.x > -Size.x &&
							xz.y < Size.y && xz.y > -Size.y);
					break;
			}


			return false;
		}

		public Vector3 LimitWorldPoint(Vector3 worldPos)
		{
			var shape_xz = transform.position.xz();
			var xz       = ShapeMatrix.inverse.MultiplyPoint3x4(worldPos).xz();

			switch (shape) {
				case Shape.Circle:
					if (xz.magnitude > Radius) {
						xz = xz.normalized * Radius;
					}
					break;

				case Shape.Rect:
					xz.x = Mathf.Clamp(xz.x, -Size.x, Size.x);
					xz.y = Mathf.Clamp(xz.y, -Size.y, Size.y);
					break;
			}


			Vector3 move_back = ShapeMatrix.MultiplyPoint3x4(new Vector3(xz.x, 0, xz.y));
			move_back.y = worldPos.y;
			return move_back;
		}

		public void LimitKCC(KinematicCharacterMotor motor)
		{
			if (!Enabled) return;

			Vector3 pos        = motor.TransientPosition;
			Vector3 limitedPos = LimitWorldPoint(pos);

			if (Vector3.Distance(pos, limitedPos) > Mathf.Epsilon) {
				motor.SetPosition(limitedPos);
			}
		}

	}
}