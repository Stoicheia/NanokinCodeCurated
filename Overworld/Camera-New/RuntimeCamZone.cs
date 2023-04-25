using System.Collections.Generic;
using Anjin.Regions;

namespace Anjin.Cameras
{
	public class RuntimeCamZone
	{
		public enum Type { Global, Spatial }

		public Type type;
		public RegionObject obj;
		public GameCameraZoneMetadata data;

		public List<CamRef> Cams;

		RuntimeCamZone() { Cams = new List<CamRef>(); }

		public RuntimeCamZone(RegionObject _obj, GameCameraZoneMetadata _data) : this() {
			type = Type.Spatial;
			obj = _obj;
			data = _data;
		}

		public RuntimeCamZone(GameCameraZoneMetadata _data) : this() {
			type = Type.Global;
			obj  = null;
			data = _data;
		}

		//public int CamPriority { get; }
		public bool ActiveOverride { get; }
	}
}