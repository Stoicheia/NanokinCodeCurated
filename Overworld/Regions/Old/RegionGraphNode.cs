namespace Anjin.Regions
{
	/*public class RegionNode : RegionObjectSpatial, IRegionGraphNode2D
	{
		public RegionShapeArea2D area;
		public IRegionShapeArea<Vector2> Area => area;

		public List<RegionNodeLink> _links;
		[ShowInInspector]
		public List<RegionNodeLink> Links
		{
			get
			{
				if (_links == null) return _links;

				/*for (int i = 0; i < _links.Count; i++)
				{
					if (ParentGraph.Links.Contains(_links[i]) && _links[i].Valid && _links[i].Alive) continue;
					Debug.Log("Removing dead link");
					_links.RemoveAt(i);
					i--;
				}#1#

				return _links;
			}
			set
			{
				if (_links == null) _links = value;
			}
		}

		//Shouldn't need to be called at runtime
		public RegionNode() 						: this(null) { }
		public RegionNode(RegionGraph parentGraph) : base(parentGraph)
		{
			Links = new List<RegionNodeLink>();
			area  = new RegionShapeArea2D(this);
		}


		public RegionNodeLink LinkToNode(RegionNode node)
		{
			RegionNodeLink link;

			//We can't link to a null node
			if (node == null) return null;

			/*
			//Check for duplicates: Our links
			for (int i = 0; i < Links.Count; i++)
			{
				link = Links[i];
				if (link.LinksTo(this)) return null;
			}

			//Check for duplicates: Our links
			for (int i = 0; i < node.Links.Count; i++)
			{
				link = node.Links[i];
				if (link.LinksTo(this)) return null;
			}
			#1#

			link = new RegionNodeLink(ParentGraph, this, node);
			Links.Add(link);
			node.Links.Add(link);

			return link;
		}

		public void Destroy()
		{
			for (int i = 0; i < Links.Count; i++)
			{
				Links[i].Destroy();
			}

			Links.Clear();
			Alive = false;
		}


		public GraphObjectTransform GetTransform() => Transform;
		//public override ISpatialLinkPosition<T> NewLinkPosition<T>() => null;
	}*/
}