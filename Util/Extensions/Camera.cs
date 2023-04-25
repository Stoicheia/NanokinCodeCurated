using UnityEngine;
using UnityUtilities;

namespace Anjin.Util {
	public static partial class Extensions {


		public static bool MosuePositionValid(this Camera camera, Vector2 mousePosition) {
			Vector3 lo = camera.ViewportToScreenPoint(Vector3.zero);
			Vector3 hi = camera.ViewportToScreenPoint(Vector3.one);

			var     screenrect = new Rect(lo, hi - lo);
			Vector2 mousepos   = mousePosition;

			return screenrect.Contains(mousepos);
		}


	}
}