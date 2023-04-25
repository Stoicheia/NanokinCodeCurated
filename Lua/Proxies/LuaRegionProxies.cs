using System.Linq;
using Anjin.Scripting;
using Anjin.Util;
using MoonSharp.Interpreter;
using UnityEngine;

namespace Anjin.Regions
{
	[MoonSharpUserData]
	public class RegionGraphLuaProxy : LuaProxy<RegionGraph>
	{

		public RegionObject find_by_path(string path) => proxy.FindByPath(path);

		public Vector3 get_obj_pos(string id)
		{
			var obj = proxy.GraphObjects.Select(x=>x as RegionObjectSpatial).WhereNotNull().FirstOrDefault(x => x.ID == id);
			if (obj != null)
				return obj.Transform.Position;
			return Vector3.zero;
		}
	}

	[LuaProxyTypes(typeof(RegionObject), typeof(RegionShape2D), typeof(RegionObjectSpatial))]
	public class RegionObjectProxyBase<T> : LuaProxy<T>
		where T : RegionObject
	{
		RegionObjectSpatial spatialTarget => proxy as RegionObjectSpatial;

		public string name 	=> proxy.Name;
		public string ID 	=> proxy.ID;

		//public IRegionMetadata metadata()

		public bool is_spatial => spatialTarget != null;
		public Vector3? position 	{ get { if (spatialTarget != null) return spatialTarget.Transform.Position; else return null; } }
		public Vector3? scale 		{ get { if (spatialTarget != null) return spatialTarget.Transform.Scale; else return null; } }

		public RegionShape2D shape2D => proxy as RegionShape2D;

		//TODO: add support for quaternions
		//public Vector3? position { get { if (spatialTarget != null) return spatialTarget.Transform.Rotation; else return null; } }
	}

	public class RegionObjectProxy : RegionObjectProxyBase<RegionObject> 	{}
	public class RegionShape2DProxy : RegionObjectProxyBase<RegionShape2D> 	{}
}