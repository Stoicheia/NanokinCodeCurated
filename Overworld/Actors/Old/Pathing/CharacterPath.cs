using System.Collections.Generic;
using UnityEngine;

namespace Anjin.Actors
{
	public class CharacterPath
	{
		public List<CharacterPathPoint> Points;

		public float GetLength()
		{
			//TODO(CL): Add length calc for other types of points
			//TODO(CL): Make this precalculated so we don't do it every time dammit
			float length = 0;
			CharacterPathPoint p;
			for (int i = 0; i < Points.Count-1; i++)
			{
				p = Points[i];
				if (p.type == CharacterPathPoint.Type.Walk)
				{
					length += Vector3.Distance(p.Position, Points[i + 1].Position);
				}
			}

			return length;
		}

		public CharacterPath()
		{
			Points = new List<CharacterPathPoint>();
		}

		public CharacterPath(List<CharacterPathPoint> _Points)
		{
			Points = _Points;
		}
	}

}