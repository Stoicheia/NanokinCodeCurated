using System;
using UnityEngine;

namespace Util.Components.Cinemachine
{
	[Serializable]
	public struct SphereCoordinate
	{
		[Range(-360, 360)] public float azimuth;
		[Range(-50, 50)]   public float elevation;
		[Range(-50, 50)]   public float distance;

		public SphereCoordinate Default => new SphereCoordinate(180, 10, 15);

		public Vector3 Vector
		{
			get => new Vector3(azimuth, elevation, distance);
			set
			{
				azimuth   = value.x;
				elevation = value.y;
				distance  = value.z;
			}
		}

		public SphereCoordinate(SphereCoordinate copy)
		{
			azimuth   = copy.azimuth;
			elevation = copy.elevation;
			distance  = copy.distance;
		}

		public SphereCoordinate(float azimuth, float elevation, float distance)
		{
			this.azimuth   = azimuth;
			this.elevation = elevation;
			this.distance  = distance;
		}

		public override string ToString()
		{
			return $"{{{azimuth}, {elevation}, {distance}}}";
		}

		public SphereCoordinate Clone()
		{
			return new SphereCoordinate(this);
		}

		public SphereCoordinate Set(float azimuth, float elevation, float distance)
		{
			this.azimuth   = azimuth;
			this.elevation = elevation;
			this.distance  = distance;

			return this;
		}

		public static SphereCoordinate operator *(SphereCoordinate coord, float v)
		{
			return new SphereCoordinate(
				coord.azimuth * v,
				coord.elevation * v,
				coord.distance * v
			);
		}

		public static SphereCoordinate operator +(SphereCoordinate coord, float v)
		{
			return new SphereCoordinate(
				coord.azimuth + v,
				coord.elevation + v,
				coord.distance + v
			);
		}

		public static SphereCoordinate operator +(SphereCoordinate coord1, SphereCoordinate coord2)
		{
			return new SphereCoordinate(
				coord1.azimuth + coord2.azimuth,
				coord1.elevation + coord2.elevation,
				coord1.distance + coord2.distance
			);
		}
	}
}