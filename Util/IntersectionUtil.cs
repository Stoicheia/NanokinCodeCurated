using UnityEngine;

namespace Util {
	public class IntersectionUtil {

		public static bool Intersection_PointBox(Vector3 point, Matrix4x4 matrix, Vector3 size)
		{
			Vector3 tpoint = matrix.inverse.MultiplyPoint3x4(point);

			if (
				tpoint.x < size.x && tpoint.x > -size.x &&
				tpoint.y < size.y && tpoint.y > -size.y &&
				tpoint.z < size.z && tpoint.z > -size.z
			) {
				return true;
			}

			return false;
		}

		// Height is size from center
		public static bool Intersection_PointCylinder(Vector3 point, Matrix4x4 matrix, float radius, float height)
		{
			Vector3 tpoint = matrix.inverse.MultiplyPoint3x4(point);

			float hmag = tpoint.xz().magnitude;

			if (tpoint.y <= height && tpoint.y >= -height && hmag <= radius) {
				return true;
			}

			return false;
		}

		public static bool Intersection_PointTriangle(Vector3 point, Matrix4x4 matrix, Vector2 v0, Vector2 v1, Vector2 v2, float verticalExpansion)
		{
			Vector3 tpoint = matrix.inverse.MultiplyPoint3x4(point);

			if (tpoint.y < -verticalExpansion || tpoint.y > verticalExpansion)
				return false;

			var hpoint = tpoint.xz();

			float d1,      d2, d3;
			bool  has_neg, has_pos;

			d1 = sign(hpoint, v0, v1);
			d2 = sign(hpoint, v1, v2);
			d3 = sign(hpoint, v2, v0);

			has_neg = (d1 < 0) || (d2 < 0) || (d3 < 0);
			has_pos = (d1 > 0) || (d2 > 0) || (d3 > 0);

			return !(has_neg && has_pos);

			float sign (Vector2 p1, Vector2 p2, Vector2 p3) => (p1.x - p3.x) * (p2.y - p3.y) - (p2.x - p3.x) * (p1.y - p3.y);
		}

		public static (bool, float) Intersection_RaySphere(Ray ray, Vector3 center, float radius)
		{
			Vector3 oc = ray.origin - center;
			float   a  = Vector3.Dot(ray.direction, ray.direction);
			float   b  = 2.0f * Vector3.Dot(oc, ray.direction);
			float   c  = Vector3.Dot(oc,        oc) - radius * radius;

			float discriminant = (b * b) - (4 * a * c);

			if (discriminant < 0) {
				return (false, -1);
			} else {
				return (true, (- b - Mathf.Sqrt(discriminant)) / (2.0f * a));
			}
		}

		public static bool Intersection_RayOBB(Ray ray, Matrix4x4 matrix, Vector3 size, out float dist)
		{
			dist = -1.0f;


			float tMin = 0.0f;
			float tMax = 1000000f;

			Vector3 aabb_min = -(size / 2) * 0.5f;
			Vector3 aabb_max = (size / 2)  * 0.5f;

			Vector3 col3       = matrix.GetColumn(3);
			Vector3 worldSpace = new Vector3(col3.x, col3.y, col3.z);
			Vector3 delta      = worldSpace - ray.origin;

			/*using(Draw.WithMatrix(matrix)){
				Draw.WireBox(Vector3.zero, size);
			}*/

			{
				Vector3 col0  = matrix.GetColumn(0);
				Vector3 xaxis = new Vector3(col0.x, col0.y, col0.z);

				float e = Vector3.Dot(xaxis,         delta);
				float f = Vector3.Dot(ray.direction, xaxis);

				if (Mathf.Abs(f) > 0.001f) {
					float t1 = (e + aabb_min.x) / f;
					float t2 = (e + aabb_max.x) / f;

					if (t1 > t2) {
						float w = t1;
						t1 = t2;
						t2 = w;
					}

					if (t2 < tMax) tMax = t2;
					if (t1 > tMin) tMin = t1;

					if (tMax < tMin)
						return false;
				} else {
					if (-e + aabb_min.x > 0.0f || -e + aabb_max.x < 0.0f)
						return false;
				}
			}

			{
				Vector3 col1  = matrix.GetColumn(1);
				Vector3 yaxis = new Vector3(col1.x, col1.y, col1.z);

				float e = Vector3.Dot(yaxis,         delta);
				float f = Vector3.Dot(ray.direction, yaxis);

				if (Mathf.Abs(f) > 0.001f) {
					float t1 = (e + aabb_min.y) / f;
					float t2 = (e + aabb_max.y) / f;

					if (t1 > t2) {
						float w = t1;
						t1 = t2;
						t2 = w;
					}

					if (t2 < tMax) tMax = t2;
					if (t1 > tMin) tMin = t1;

					if (tMax < tMin)
						return false;
				} else {
					if (-e + aabb_min.y > 0.0f || -e + aabb_max.y < 0.0f)
						return false;
				}
			}

			{
				Vector3 col2  = matrix.GetColumn(2);
				Vector3 zaxis = new Vector3(col2.x, col2.y, col2.z);

				float e = Vector3.Dot(zaxis,         delta);
				float f = Vector3.Dot(ray.direction, zaxis);

				if (Mathf.Abs(f) > 0.001f) {
					float t1 = (e + aabb_min.z) / f;
					float t2 = (e + aabb_max.z) / f;

					if (t1 > t2) {
						float w = t1;
						t1 = t2;
						t2 = w;
					}

					if (t2 < tMax) tMax = t2;
					if (t1 > tMin) tMin = t1;

					if (tMax < tMin)
						return false;
				} else {
					if (-e + aabb_min.z > 0.0f || -e + aabb_max.z < 0.0f)
						return false;
				}
			}

			dist = tMin;

			return true;
		}

		public static bool GetPlaneIntersection(Ray ray, Vector3 pos, Vector3 scale, Vector3 rot, Vector2 size, out float dist, float forgiveness)
		{
			Quaternion qrot      = Quaternion.Euler(rot);
			Plane      areaPlane = new Plane( qrot * Vector3.up, pos);
			areaPlane.Raycast(ray, out float enter);

			Vector3 ray_point = ray.GetPoint(enter);

			var correctedPos = ray_point - pos;
			var projectedVec = Vector3.ProjectOnPlane(correctedPos, qrot.normalized * Vector3.up);

			//Take the rotation out
			var rotationCorrection = Matrix4x4.Rotate(Quaternion.Euler(rot)).inverse.MultiplyVector(projectedVec);
			var v                  = (new Vector2(rotationCorrection.x, rotationCorrection.z) / size) / scale.xy();

			dist = Vector3.Distance(ray_point, ray.origin);


			bool yes =  Mathf.Abs(v.x) <= 1 + forgiveness && Mathf.Abs(v.y) <= 1 + forgiveness;

			return yes;
		}

		public static bool GetDiskIntersection(Ray ray, Vector3 pos, Vector3 scale, Vector3 rot, float radius, out float dist)
		{
			Quaternion qrot      = Quaternion.Euler(rot);
			Plane      areaPlane = new Plane( qrot * Vector3.up, pos);
			areaPlane.Raycast(ray, out float enter);

			Vector3 ray_point = ray.GetPoint(enter);

			var correctedPos = ray_point - pos;
			var projectedVec = Vector3.ProjectOnPlane(correctedPos, qrot.normalized * Vector3.up);

			//Take the rotation out
			var rotationCorrection = Matrix4x4.Rotate(Quaternion.Euler(rot)).inverse.MultiplyVector(projectedVec);
			var v                  = (new Vector2(rotationCorrection.x, rotationCorrection.z) / radius) / scale.xy();

			dist = Vector3.Distance(ray_point, ray.origin);

			bool yes = v.magnitude <= 1;

			return yes;
		}


		// https://en.wikipedia.org/wiki/M%C3%B6ller%E2%80%93Trumbore_intersection_algorithm#:~:text=The%20M%C3%B6ller%E2%80%93Trumbore%20ray%2Dtriangle,the%20plane%20containing%20the%20triangle.

		public static bool GetTriangleIntersection(Ray ray, Vector3 pos, Vector3 scale, Vector3 rot, Vector3 v0, Vector3 v1, Vector3 v2, out float dist)
		{
			dist = Mathf.Infinity;

			var matrix = Matrix4x4.TRS(pos, Quaternion.Euler(rot), scale);

			v0 = matrix.MultiplyPoint3x4(v0);
			v1 = matrix.MultiplyPoint3x4(v1);
			v2 = matrix.MultiplyPoint3x4(v2);

			/*ray.origin    = ray.origin - pos;
			ray.direction = Matrix4x4.Rotate(Quaternion.Euler(rot)).inverse.MultiplyVector(ray.direction);*/

			// compute plane's normal
			Vector3 edge1 = v1 - v0;
			Vector3 edge2 = v2 - v0;

			Vector3 h = Vector3.Cross(ray.direction, edge2);
			float   a = Vector3.Dot(edge1, h);

			// Parallel test
			if (a > -Mathf.Epsilon && a < Mathf.Epsilon) return false;

			float   f = 1.0f / a;
			Vector3 s = ray.origin - v0;
			float   u = f * Vector3.Dot(s, h);

			if (u < 0.0f || u > 1.0f) return false;

			Vector3 q = Vector3.Cross(s, edge1);
			float   v = Vector3.Dot(ray.direction, q) * f;

			if (v < 0.0f || u + v > 1.0f) return false;

			dist = f * Vector3.Dot(edge2, q);

			if (dist > Mathf.Epsilon) {
				return true;
			}
			return false;
		}
	}
}