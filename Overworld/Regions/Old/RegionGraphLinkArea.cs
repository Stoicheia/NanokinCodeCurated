namespace Anjin.Regions
{

	/// <summary>
	/// Defines an area for a link.
	/// Line coords are measured from Node1.
	/// Width coords are measured from the center.
	/// AreaPos: (x, 0) if a line
	/// 		 (x, y) if a line with width.
	///
	/// Transform does not matter with this object type.
	/// </summary>
	/*public class RegionShapeLinkArea : IRegionShapeArea<Vector2>
	{
		public RegionNodeLink parent;

		/*public string id    { get; }
		public bool  Alive { get; private set; }#1#

		public enum LinkAreaType { Line, Plane }
		public LinkAreaType Type;

		public float PlaneWidth;
		public Vector2 PlaneNormal;

		public RegionShapeLinkArea(RegionNodeLink parent, LinkAreaType type = LinkAreaType.Line)
		{
			/*id = DataUtil.MakeShortID(4);
			Alive = true;#1#

			this.parent = parent;
			Type = type;

			PlaneWidth = 1;
			PlaneNormal = Vector2.up;
		}

		//public void Destroy() => Alive = false;

		public Vector2 AreaPosToNormalizedPos(Vector2 areaPos)
		{
			var dist = parent.GetLength();

			switch(Type)
			{
				case LinkAreaType.Line:  return new Vector2(areaPos.x / dist, 0);
				case LinkAreaType.Plane: return new Vector2(areaPos.x / dist, areaPos.y / PlaneWidth);
			}

			return Vector2.zero;
		}

		public Vector2 NormalizedPosToAreaPos(Vector2 normalizedPos)
		{
			var dist = parent.GetLength();

			switch(Type)
			{
				case LinkAreaType.Line:  return new Vector2(normalizedPos.x * dist, 0);
				case LinkAreaType.Plane: return new Vector2(normalizedPos.x * dist, normalizedPos.y * PlaneWidth);
			}

			return Vector2.zero;
		}

		public Vector3 AreaPosToWorldPos(Vector2 areaPos)
		{
			var length = parent.GetLength();
			var dir = parent.GetDirection();

			var origin = parent.Node1Position.GetWorldPosition(parent.Node1);

			Vector3 areaPos3D = new Vector3(areaPos.x*length, 0, areaPos.y);

			//var rotation = Matrix4x4.Rotate(Quaternion.Euler(0,180-MathUtil.Angle(new Vector2(dir.x,dir.z)),0)).MultiplyVector(areaPos3D);

			Vector3 final = areaPos3D + origin;

			//Debug.Log(areaPos + " " + areaPos3D + " " + rotation + " " + dir);

			return final;
		}

		public Vector2 WorldPosToAreaPos(Vector3 worldPos)
		{
			var length = parent.GetLength();

			var correctedPos = worldPos - parent.Node1Position.GetWorldPosition(parent.Node1);
			var normal = parent.GetDirection();

			normal = ( Matrix4x4.Rotate(Quaternion.Euler(normal)).inverse.MultiplyVector(
				Type == LinkAreaType.Line ?
					Vector3.up :
					new Vector3(PlaneNormal.x, PlaneNormal.y, 0).normalized
					 ));

			var planeVec = Vector3.ProjectOnPlane(correctedPos,  normal);

			if (Type == LinkAreaType.Line)  return new Vector2(planeVec.x/length, planeVec.z );
			if (Type == LinkAreaType.Plane) return new Vector2(planeVec.x/length, planeVec.z / PlaneWidth);

			else return Vector2.zero;
		}

		public Vector2 GetRandomAreaPointInside()
		{
			return Vector2.zero;
		}

		public Vector3 GetRandomWorldPointInside()
		{
			return Vector2.zero;
		}


	}*/
}