using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Util
{
	public static class Geometry
	{
		public static Vector3 RandomCone(float radius, float angle, bool onRad = false, bool onCone = false)
		{
			float azimuthal = UnityEngine.Random.Range(0, 2*Mathf.PI);
			float polar = onCone ? Mathf.Deg2Rad * angle : Mathf.Deg2Rad * UnityEngine.Random.Range(0, angle);
			Vector3 result = UnityEngine.Random.Range(radius * Convert.ToInt32(!onRad), radius) * new Vector3(
				Mathf.Cos(azimuthal) * Mathf.Sin(polar),
				Mathf.Cos(polar),
				Mathf.Sin(azimuthal) * Mathf.Sin(polar));
			return result;
		}
	}

}
