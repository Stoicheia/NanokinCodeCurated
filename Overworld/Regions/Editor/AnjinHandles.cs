using System.Collections.Generic;
using Anjin.Regions;
using Anjin.Util;
using Drawing;
using JetBrains.Annotations;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Util;

namespace Anjin.CustomHandles {

	public enum ShapeType {
		Sphere, Box, Plane, Disk, Triangle,
	}

	public struct HandleShape {

		public string ID;

		public Vector3 pos;
		public Vector3 rot;
		public Vector3 scale;

		//public Matrix4x4 transform;

		public ShapeType type;
		public float     radius;
		public Vector3   box_size;
		public Vector2   plane_size;

		public Vector3 tri_1;
		public Vector3 tri_2;
		public Vector3 tri_3;
	}

	public struct AnjinHandlesEvent {
		public bool    lmb;
		public bool    rmb;
		public Vector3 intersection;
	}

	/*	TODO:
	 *		Dragging support for every handle (free or axis-limited).
	 * 		Replicating the default unity transform handles! (Still use for Handles rendering, ALINE doesn't have what we need)
	 *		Shapes: Cone? Cylinder?
	 * 		Scale/Rot support on sphere.
	 * 		Support across multiple editors; singleton instead of instances like Unity handles?
	 */


	public class AnjinHandles {

		public const string BASE_ID = "BASE";

		public List<HandleShape>       Shapes      = new List<HandleShape>();
		public Dictionary<string, int> IDsToShapes = new Dictionary<string, int>();

		public bool IsCollecting;

		public Stack<string> CurrentIDStack = new Stack<string>();
		public string        GetCurrentID() => CurrentIDStack.JoinString("_");

		public (string ID, bool ok) ClosestID;

		public const float SELECTION_FORGIVENESS = 0.005f;

		public void BeginFrame()
		{
			switch (Event.current.type) {
				case EventType.Layout:
					Shapes.Clear();

					CurrentIDStack.Clear();
					CurrentIDStack.Push(BASE_ID);

					IDsToShapes.Clear();

					ClosestID = ("", false);

					//HandleUtility.AddDefaultControl(1);
					break;

			}
		}

		public void EndFrame()
		{
			switch (Event.current.type)
			{
				case EventType.Layout:
					Vector3 lo = Camera.current.ViewportToScreenPoint(Vector3.zero);
					Vector3 hi = Camera.current.ViewportToScreenPoint(Vector3.one);

					var     screenrect = new Rect(lo, hi - lo);
					Vector2 mousepos   = Event.current.mousePosition;

					// This condition fixes an annoyinug bug on linux (Screen position out of view frustum) whenever the mouse enters the SceneView
					if (screenrect.Contains(mousepos))
					{
						ClosestID = GetShapeForRay(HandleUtility.GUIPointToWorldRay(mousepos), out float distance);
						if (ClosestID.ok)
						{
							HandleUtility.AddControl(ClosestID.ID.GetHashCode(), 1);
						}
					}

					break;
			}
		}

		public void PushID(string id) => CurrentIDStack.Push(id);
		public void PopID()           => CurrentIDStack.Pop();


		public bool DoPlane(GraphObjectTransform transform, Vector2 size, [CanBeNull] string id, out AnjinHandlesEvent ev)
			=> DoPlane(transform.Position, transform.Rotation.eulerAngles, transform.Scale, size, id, out ev);

		public bool DoPlane(Vector3 position, Vector3 rotation, Vector3 scale, Vector2 size, [CanBeNull] string id, out AnjinHandlesEvent ev)
			=> DoShape(new HandleShape {type = ShapeType.Plane, plane_size = size, pos = position, rot = rotation, scale = scale}, id, out ev);


		public bool DoDisk(GraphObjectTransform transform, float radius, [CanBeNull] string id, out AnjinHandlesEvent ev)
			=> DoDisk(transform.Position, transform.Rotation.eulerAngles, transform.Scale, radius, id, out ev);

		public bool DoDisk(Vector3 position, Vector3 rotation, Vector3 scale, float radius, [CanBeNull] string id, out AnjinHandlesEvent ev)
			=> DoShape(new HandleShape {type = ShapeType.Disk, radius = radius, pos = position, rot = rotation, scale = scale}, id, out ev);

		public bool DoSphere(Vector3 position, float radius, string id, out AnjinHandlesEvent ev)
			=> DoShape(new HandleShape {type = ShapeType.Sphere, radius = radius, pos = position}, id, out ev);

		public bool DoTriangle(GraphObjectTransform transform, Vector3 p1, Vector3 p2, Vector3 p3, [CanBeNull] string id, out AnjinHandlesEvent ev)
			=> DoTriangle(transform.Position, transform.Rotation.eulerAngles, transform.Scale, p1, p2, p3, id, out ev);

		public bool DoTriangle(Vector3 position, Vector3 rotation, Vector3 scale, Vector3 p1, Vector3 p2, Vector3 p3, [CanBeNull] string id, out AnjinHandlesEvent ev) =>
			DoShape(new HandleShape {type = ShapeType.Triangle, pos = position, rot = rotation, scale = scale, tri_1 = p1, tri_2 = p2, tri_3 = p3}, id, out ev);

		public bool DoShape(HandleShape shape, [CanBeNull] string id, out AnjinHandlesEvent ev)
		{
			ev = new AnjinHandlesEvent();
			string ID =  (id ?? "") + "_" + GetCurrentID();

			shape.ID = ID;

			var hash = shape.ID.GetHashCode();

			if (Event.current.type == EventType.Layout) {
				Shapes.Add(shape);
				IDsToShapes[ID] = Shapes.Count - 1;
				/*} else if (Event.current.OnRepaint()) {
					Handles.Label(shape.pos, ID +": " + hash);*/
			} else {
				return ClosestID.ok && ClosestID.ID == ID && HandleUtility.nearestControl == hash;
			}

			return false;
		}

		public bool DoShapeWithFullID(HandleShape shape, string ID, out AnjinHandlesEvent ev)
		{
			ev = new AnjinHandlesEvent();

			shape.ID = ID;

			var hash = shape.ID.GetHashCode();

			if (Event.current.type == EventType.Layout) {
				Shapes.Add(shape);
				IDsToShapes[ID] = Shapes.Count - 1;
				/*} else if (Event.current.OnRepaint()) {
					Handles.Label(shape.pos, ID +": " + hash);*/
			} else {
				return ClosestID.ok && ClosestID.ID == ID && HandleUtility.nearestControl == hash;
			}

			return false;
		}

		public (string id, bool ok) GetShapeForRay(Ray ray, out float distance)
		{
			float closestDistance = Mathf.Infinity;
			int   closestInd      = -1;

			HandleShape shape;
			for (int i = 0; i < Shapes.Count; i++) {
				shape = Shapes[i];
				switch (shape.type) {
					case ShapeType.Sphere:
						(bool, float) hit = IntersectionUtil.Intersection_RaySphere(ray, shape.pos, shape.radius);
						if (hit.Item1) {
							if (hit.Item2 < closestDistance) {
								closestDistance = hit.Item2;
								closestInd      = i;
							}
						}

						break;
					case ShapeType.Box: {

						Matrix4x4 mat = Matrix4x4.TRS(shape.pos, Quaternion.Euler(shape.rot), Vector3.one);
						if (IntersectionUtil.Intersection_RayOBB(ray, mat,
																 new Vector3(shape.box_size.x * shape.scale.x,
																			 shape.box_size.y * shape.scale.y,
																			 shape.box_size.z * shape.scale.z),
																 out float dist)) {

							if (dist < closestDistance) {
								closestDistance = dist;
								closestInd      = i;
							}
						}
					}
						break;

					case ShapeType.Plane: {

						if (IntersectionUtil.GetPlaneIntersection(ray, shape.pos, shape.scale, shape.rot, shape.plane_size, out float dist, SELECTION_FORGIVENESS)) {
							if (dist < closestDistance) {
								closestDistance = dist;
								closestInd      = i;
							}
						}
					}
						break;

					case ShapeType.Disk: {
						if (IntersectionUtil.GetDiskIntersection(ray, shape.pos, shape.scale, shape.rot, shape.radius, out float dist)) {
							Vector3 pt = ray.GetPoint(dist);
							if (dist < closestDistance) {
								closestDistance = dist;
								closestInd      = i;
							}
						}

					} break;

					case ShapeType.Triangle: {
						if (IntersectionUtil.GetTriangleIntersection(ray, shape.pos, shape.scale,shape.rot, shape.tri_1, shape.tri_2, shape.tri_3, out float dist)) {
							Vector3 pt = ray.GetPoint(dist);
							if (dist < closestDistance) {
								closestDistance = dist;
								closestInd      = i;
							}
						}
					} break;
				}
			}

			/*if (closestInd != -1) {
				var pt = ray.GetPoint(closestDistance);
				Handles.Label(pt + Vector3.down * 0.25f, closestInd.ToString());
				Draw.Cross(pt, 0.5f, Color.red);
			}*/

			distance = closestDistance;

			if (closestInd == -1) return ("", false);
			return (Shapes[closestInd].ID, true);
		}



	}
}