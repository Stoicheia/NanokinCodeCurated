using Combat.Data.VFXs;
using UnityEngine;

namespace Combat.Toolkit
{
	public class ScaleVFX : VFX
	{
		public Vector3 scale;

		public ScaleVFX(Vector3 scale)
		{
			this.scale = scale;
		}

		public ScaleVFX(float scale)
		{
			this.scale = Vector3.one * scale;
		}

		public override Vector3 VisualScale => scale;
	}
}