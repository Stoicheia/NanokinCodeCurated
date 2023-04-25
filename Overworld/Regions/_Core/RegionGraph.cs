using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;

namespace Anjin.Regions
{
	public enum GraphObjectSection { Objects, SpatialLinks, Sequences, Metadata }

	/// <summary>
	/// An object in a region graph. Designates a data=markup object, but this base does not mandate a spatial transform.
	/// </summary>
	public class RegionObject
	{
		public RegionGraph   ParentGraph;
		public GraphLayerRef Layer;

		public string Name;

		[SerializeField, HideInInspector]
		private string id;
		[ShowInInspector]
		public string ID => id;

		public bool   Alive;

		public List<RegionMetadata> Metadata;

		public RegionObject()
		{
			Name  = "";
			id = DataUtil.MakeShortID(6);
			Alive = true;
			Layer = GraphLayerRef.NullRef;
			Metadata = new List<RegionMetadata>();
		}

		public RegionObject(RegionGraph parentGraph) : this()
		{
			ParentGraph = parentGraph;
		}

		public void InsureMetadataList() { if (Metadata == null) Metadata = new List<RegionMetadata>(); }

		public bool TryGetMetadata<T>(out T meta)
			where T:RegionMetadata
		{
			meta = null;
			for (int i = 0; i < Metadata.Count; i++) {
				if (Metadata[i] is T found) {
					meta = found;
					return true;
				}
			}
			return false;
		}
	}

	public struct RegionObjectRef
	{
		public string 	ID;
		public int 		MetaDataIndex;
		public GraphObjectSection Section;

		public RegionObjectRef(string id)
		{
			ID            = id;
			MetaDataIndex = -1;
			Section 	  = GraphObjectSection.Objects;
		}

		public RegionObjectRef(string id, int metaDataIndex)
		{
			ID = id;
			MetaDataIndex = metaDataIndex;
			Section = GraphObjectSection.Objects;
		}

		public RegionObjectRef(string id, int metaDataIndex, GraphObjectSection section)
		{
			ID            = id;
			MetaDataIndex = metaDataIndex;
			Section       = section;
		}

		public const string NullID = "$NULL$";
		public static RegionObjectRef Default = new RegionObjectRef(NullID, -1);
	}

	/// <summary>
	/// A region object that has a 3D transform, so it can actually be placed somewhere in a scene.
	/// </summary>
	public abstract class RegionObjectSpatial : RegionObject
	{
		public GraphObjectTransform Transform;

		// NOTE: This is specifically for runtime region objects, and not ones created in the region editor.
		[NonSerialized]
		public Transform RuntimeTransform;

		public bool RequiresPathfinding = false;

		public RegionObjectSpatial() 						: this(null) { }
		public RegionObjectSpatial(RegionGraph parentGraph) : base(parentGraph)
		{
			Transform = new GraphObjectTransform();
		}

		public abstract bool IsPointOverlapping(Vector3 point);

		public virtual Vector3 GetFocusPos() => Transform != null ? Transform.Position : Vector3.zero;

		//public abstract ISpatialLinkPosition<T> NewLinkPosition<T>() where T: RegionObjectSpatial;
	}

	/// <summary>
	/// A graph for layout out generalized data-markup objects in a 3D game level.
	/// </summary>
	public class RegionGraph
	{
		public List<RegionObject> 			GraphObjects;
		public List<RegionLink> 			Links;
		public List<RegionSpatialLinkBase> 	SpatialLinks;
		public List<RegionObjectSequence> 	Sequences;
		public List<RegionMetadata> 		GlobalMetadata;

		public List<GraphLayer> 			Layers;

		public RegionGraph()
		{
			GraphObjects   = new List<RegionObject>();
			Links 		   = new List<RegionLink>();
			SpatialLinks   = new List<RegionSpatialLinkBase>();
			Sequences 	   = new List<RegionObjectSequence>();
			GlobalMetadata = new List<RegionMetadata>();

			Layers 		 = new List<GraphLayer>();
			Layers.Add(new GraphLayer("Base"));
		}

		public R FindObject<R>(string ID) where R:RegionObject
		{
			R obj = GraphObjects.FirstOrDefault(x => x.ID == ID) as R;
			if (obj == null) obj = Links.FirstOrDefault(x => x.ID == ID) as R;
			if (obj == null) obj = SpatialLinks.FirstOrDefault(x => x.ID == ID) as R;
			if (obj == null) obj = Sequences.FirstOrDefault(x => x.ID == ID) as R;

			return obj;
		}

		public void RemoveObject(RegionObject obj)
		{
			if (obj is RegionLink link) {
				Links.Remove(link);
			} else if (obj is RegionSpatialLinkBase slink) {
				SpatialLinks.Remove(slink);
			} else if (obj is RegionObjectSequence seq) {
				Sequences.Remove(seq);
			} else {
				if(!GraphObjects.Exists(x=>x.ID == obj.ID)) return;
				TrimLinks(obj);
				GraphObjects.Remove(obj);
			}

		}

		void TrimLinks(RegionObject obj)
		{
			for (int i = 0; i < Links.Count; i++) {
				if (Links[i].First == obj || Links[i].Second == obj) {
					Links[i].First  = null;
					Links[i].Second = null;
					Links.RemoveAt(i);
				}
			}

			for (int i = 0; i < SpatialLinks.Count; i++) {
				if (SpatialLinks[i].FirstID == obj.ID || SpatialLinks[i].SecondID == obj.ID) {
					SpatialLinks.RemoveAt(i--);
				}
			}
		}

		public RegionSpatialLinkBase LinkSpatialObjects(ILinkableBase obj1, ILinkableBase obj2)
		{
			var Link = obj1.LinkTo(obj2);
			//if (Links.Contains(Link)) return null;

			//Debug.Log($"Generate link between nodes {obj1.Name} and {obj2.Name}");

			if(Link != null)
				SpatialLinks.Add(Link);

			return Link;
		}


		//TODO: Do anything literally anything better than this!
		public RegionShape2DLink[] Get2DLinksForShape(RegionShape2D shape)
		{
			if (shape != null)
			{
				return SpatialLinks.OfType<RegionShape2DLink>().Where(x => x.LinksTo(shape)).ToArray();
			}

			return new RegionShape2DLink[0];
		}

		public RegionObject FindByPath(string path)
		{
			if (path[0] == '$') {
				var id = path.Substring(1);
				var obj = GraphObjects.FirstOrDefault(x => x.ID == id);
				return obj;
			}
			else {
				var obj = GraphObjects.FirstOrDefault(x => x.Name == path);
				return obj;
			}

			return null;
		}
	}

	public class GraphObjectTransform
	{
		public GraphObjectTransform()
		{
			Position = Vector3.zero;
			Rotation = Quaternion.identity;
			Scale    = Vector3.one;
		}

		public GraphObjectTransform(Vector3 position, Quaternion rotation, Vector3 scale)
		{
			Position = position;
			Rotation = rotation;
			Scale    = scale;
		}

		public Vector3    Position;
		public Quaternion Rotation;
		public Vector3    Scale;

		public Matrix4x4 matrix => Matrix4x4.TRS(Position, Rotation, Scale);

		public Vector3 Forward => 	Rotation * Vector3.forward;
		public Vector3 Up => 		Rotation * Vector3.up;

		public Vector3 TransformPoint(Vector3 point) => matrix.MultiplyPoint3x4(point);
	}

	public struct GraphLayer
	{
		public const string NULL_ID = "$NULL$";

		public string name;
		public string ID;

		public GraphLayer(string _name)
		{
			name = _name;
			ID   = DataUtil.MakeShortID(6);
		}
	}

	public struct GraphLayerRef
	{
		public string ID;
		public GraphLayerRef(string id) { ID = id; }

		public static GraphLayerRef NullRef = new GraphLayerRef()
		{
			ID = GraphLayer.NULL_ID
		};
	}


	public interface IRegionGraphObjectSpatial<T>
	{
		GraphObjectTransform GetTransform();
		IRegionShapeArea<T>  Area { get; }
	}

	public interface IRegionGraphNode2D : IRegionGraphObjectSpatial<Vector2> { }
	public interface IRegionGraphNode3D : IRegionGraphObjectSpatial<Vector3> { }

	public interface IRegionShapeArea<T>
	{
		T NormalizedPosToAreaPos(T normalizedPos);
		T AreaPosToNormalizedPos(T areaPos);

		Vector3 AreaPosToWorldPos(T       areaPos);
		T       WorldPosToAreaPos(Vector3 worldPos);
		T       GetRandomAreaPointInside();
		Vector3 GetRandomWorldPointInside();
	}

}