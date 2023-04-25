using Combat.Data.VFXs;
using UnityEngine;

namespace Combat.Toolkit
{
	public class ManualVFX : VFX
	{
		public Color tint          = Color.white;
		public Color fill          = Color.clear;
		public float emissionPower = 0;
		public float opacity       = 1;

		public override Color Tint          => tint;
		public override Color Fill       => fill;
		public override float EmissionPower => emissionPower;
		public override float Opacity       => opacity;
	}
}