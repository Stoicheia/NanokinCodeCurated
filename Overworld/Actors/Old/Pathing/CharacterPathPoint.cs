using UnityEngine;
using UnityEngine.Experimental.AI;

namespace Anjin.Actors
{
	public struct CharacterPathPoint
	{
		public enum Type { Walk, FallDown, JumpUp, JumpAcross }
		public Type type;

		public PolygonId Polygon;
		public Vector3   Position;

		public CharacterPathPoint(Type type, PolygonId polygon, Vector3 position)
		{
			this.type = type;
			Polygon   = polygon;
			Position  = position;
		}
	}
}