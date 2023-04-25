using System.Collections.Generic;
using UnityEngine;

namespace Anjin.Regions
{
	/// <summary>
	/// Defines a type of abstract link between two region objects, without anything spatial.
	/// </summary>
	public class RegionLink : RegionObject
	{
		public RegionObject First;
		public RegionObject Second;
	}


	public interface ISpatialLinkPosition<T>
		where T : RegionObjectSpatial
	{
		Vector3 				GetWorldPosition(T obj);
	}

	public interface ILinkableBase
	{
		RegionSpatialLinkBase LinkTo(ILinkableBase other);
		//List<RegionSpatialLinkBase> Links { get; }
	}

	public interface ILinkable<T, Pos> : ILinkableBase
		where T   : RegionObjectSpatial
		where Pos : ISpatialLinkPosition<T>
	{
		Pos NewLinkPosition();
	}

	public abstract class RegionSpatialLinkBase : RegionObjectSpatial
	{
		protected RegionSpatialLinkBase() : this(null) { }
		protected RegionSpatialLinkBase(RegionGraph parentGraph) : base(parentGraph) { }

		public abstract string FirstID  { get; }
		public abstract string SecondID { get; }
	}

	/// <summary>
	/// Defines a generalized link between two spatial region objects
	/// </summary>
	public abstract class RegionSpatialLink<ObjType, LinkPointPos> : RegionSpatialLinkBase, IEqualityComparer<RegionSpatialLink<ObjType, LinkPointPos>>
		where ObjType 	   : RegionObjectSpatial, ILinkable<ObjType, ISpatialLinkPosition<ObjType>>
		where LinkPointPos : struct, ISpatialLinkPosition<ObjType>
	{
		public ObjType First;
		public ObjType Second;

		public LinkPointPos FirstTransform;
		public LinkPointPos SecondTransform;

		public bool Valid => First != null && Second != null;

		public override string FirstID  => First.ID;
		public override string SecondID => Second.ID;

		public RegionSpatialLink(ObjType first, ObjType second) : this(null, first, second) { }
		public RegionSpatialLink(RegionGraph parentGraph, ObjType first, ObjType second) : base(parentGraph)
		{
			First = first;
			Second = second;

			FirstTransform = (LinkPointPos)First.NewLinkPosition();
			SecondTransform = (LinkPointPos)Second.NewLinkPosition();
		}

		public bool LinksTo(ObjType obj) => obj == First || obj == Second;

		public ObjType GetOpposite(ObjType node)
		{
			if (!Valid) return null;
			if (node == First) return Second;
			if (node == Second) return First;
			return null;
		}

		public LinkPointPos? GetPosition(ObjType node)
		{
			if (node == First) return FirstTransform;
			if (node == Second) return SecondTransform;
			return null;
		}

		/*public Vector2 GetAreaPosFromLinkPos(RegionNode node, ParkAIGraphNodeLinkPosition pos)
		{
			var worldPos = pos.GetWorldPosition(node);
			return area.WorldPosToAreaPos(worldPos);
		}*/

		public float   GetLength()    => Vector3.Distance(FirstTransform.GetWorldPosition(First), SecondTransform.GetWorldPosition(Second));
		public Vector3 GetDirection() => (SecondTransform.GetWorldPosition(Second) - FirstTransform.GetWorldPosition(First)).normalized;

		public bool Equals(RegionSpatialLink<ObjType, LinkPointPos> x, RegionSpatialLink<ObjType, LinkPointPos> y)
		{
			if ((object)x == null && (object)y == null) return true;
			if ((object)x == null || (object)y == null) return false;
			return
				( x.First == y.First && x.Second == y.Second ) ||
				( x.First == y.Second && x.Second == y.First );
		}

		public static bool operator ==(RegionSpatialLink<ObjType, LinkPointPos> x, RegionSpatialLink<ObjType, LinkPointPos> y)
		{
			if ((object)x == null && (object)y == null) return true;
			if ((object)x == null || (object)y == null) return false;
			return x.Equals(y);
		}

		public static bool operator !=(RegionSpatialLink<ObjType, LinkPointPos> x, RegionSpatialLink<ObjType, LinkPointPos> y)
		{
			if ((object) x == null) return false;
			return !x.Equals(y);
		}

		public int GetHashCode(RegionSpatialLink<ObjType, LinkPointPos> obj) => obj.First.GetHashCode() + obj.Second.GetHashCode();
	}
}