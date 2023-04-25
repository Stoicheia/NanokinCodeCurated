using API.PropertySheet;
using UnityEngine;

namespace Combat.Data.VFXs
{
	public struct VFXState
	{
		public Vector3         offset;
		public Color           tint;
		public Color           fill;
		public float           emissionPower;
		public string          animSet;
		public PuppetAnimation puppetSet;
		public string          puppetSetMarkerStart;
		public string          puppetSetMarkerEnd;
		public bool            animFreeze;
		public float           opacity;
		public Vector3         scale;

		public static VFXState Default = new VFXState
		{
			offset        = Vector3.zero,
			tint          = Color.white,
			fill          = Color.clear,
			emissionPower = 1,
			animSet       = null,
			animFreeze    = false,
			opacity       = 1,
			scale         = Vector3.one
		};

		public void Clear()
		{
			offset        = Vector3.zero;
			tint          = Color.white;
			fill          = Color.clear;
			emissionPower = 1;
			animSet       = null;
			animFreeze    = false;
			opacity       = 1;
			scale         = Vector3.one;
		}
	}
}