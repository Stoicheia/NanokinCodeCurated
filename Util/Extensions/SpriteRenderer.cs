using UnityEngine;

namespace Anjin.Util
{
	public static partial class Extensions
	{
		private static readonly int _spOverlayProp = Shader.PropertyToID("_OverlayColor");
		private static readonly int _spOpacityProp = Shader.PropertyToID("_Opacity");
		private static readonly int _spPowerProp   = Shader.PropertyToID("_EmissionPower");

		//public static void Opacity(this   SpriteRenderer sr, float opacity) => sr.sharedMaterial.SetFloat(_spOpacityProp, opacity);
		public static void Opacity(this SpriteRenderer sr, float opacity) => sr.material.SetFloat(_spOpacityProp, opacity);
		public static void ColorTint(this SpriteRenderer sr, Color color)   => sr.color = color;

		//public static void ColorFill(this     SpriteRenderer sr, Color color)   => sr.sharedMaterial.SetColor(_spOverlayProp, color);
		//public static void EmissionPower(this SpriteRenderer sr, float power)   => sr.sharedMaterial.SetFloat(_spPowerProp, power);
		public static void ColorFill(this SpriteRenderer sr, Color color) => sr.material.SetColor(_spOverlayProp, color);
		public static void EmissionPower(this SpriteRenderer sr, float power) => sr.material.SetFloat(_spPowerProp, power);

		public static SpriteRenderer Sprite(this SpriteRenderer sr, Sprite sprite)
		{
			sr.sprite = sprite;
			return sr;
		}

		public static SpriteFlips GetFlips(this SpriteRenderer sr)
		{
			return new SpriteFlips(sr.flipX, sr.flipY);
		}

		public static SpriteRenderer SetFlips(this SpriteRenderer sr, SpriteFlips flips)
		{
			sr.flipX = flips.x;
			sr.flipY = flips.y;
			return sr;
		}
	}
}