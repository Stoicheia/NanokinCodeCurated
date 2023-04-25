using JetBrains.Annotations;
using UnityEngine;
using Util;

namespace Anjin.Util
{
	public static partial class Extensions
	{
		/// <summary>
		/// Get the size of this sprite.
		/// </summary>
		public static Vector2 size([NotNull] this Sprite sprite) => new Vector2(sprite.w(), sprite.h());

		/// <summary>
		/// Get the width of this sprite.
		/// </summary>
		public static int w([NotNull] this Sprite sprite)
		{
			return (int) sprite.rect.width;
		}

		/// <summary>
		/// Get the height of this sprite.
		/// </summary>
		public static int h([NotNull] this Sprite sprite)
		{
			return (int) sprite.rect.height;
		}

		/// <summary>
		/// Get the rect representing this sprite's UV coordinates. (in range [0, 1])
		/// </summary>
		public static Rect src(this Sprite sprite)
		{
			if (sprite == null) return new Rect();

			Rect    src = sprite.rect; // as reported by unity, it is in pixels whereas we want UV coords.
			Texture tex = sprite.texture;

			return new Rect(
				Mathf.Floor(src.x)      / tex.width,
				Mathf.Floor(src.y)      / tex.height,
				Mathf.Floor(src.width)  / tex.width,
				Mathf.Floor(src.height) / tex.height
			);
		}

		public static Sprite SubSprite(this Sprite sprite, Rect r) => sprite.SubSprite(r.position, r.size);

		/// <summary>
		/// Returns a new subsprite of this sprite defined by the src rect.
		/// </summary>
		/// <param name="sprite">The parent sprite.</param>
		/// <param name="pos"></param>
		/// <param name="size"></param>
		/// <returns></returns>
		public static Sprite SubSprite(this Sprite sprite, Vector2 pos, Vector3 size)
		{
			Rect parentsrc = sprite.rect;

			Rect subsrc = new Rect(parentsrc.position, size);
			subsrc.y += parentsrc.height - pos.y - size.y;
			subsrc.x += pos.x;

			Sprite ret = UnityEngine.Sprite.Create(sprite.texture, subsrc, Vector2.one * 0.5f, sprite.pixelsPerUnit);
			return ret;
		}

		public static SpriteRenderer Material(this SpriteRenderer sr, Material material)
		{
			sr.material = material;
			return sr;
		}

		public static SpriteRenderer DefaultMaterial(this SpriteRenderer sr)
		{
			sr.material = DefaultSpriteMaterial.New;
			return sr;
		}

		public static SpriteRenderer Material(this SpriteRenderer sr)
		{
			sr.material = DefaultSpriteMaterial.New;
			return sr;
		}

		/// <summary>
		/// Fluent interface for setting the X flip of this sprite renderer.
		/// </summary>
		/// <param name="sr"></param>
		/// <param name="state">The state of the X flip. Left at null to toggle instead.</param>
		/// <returns></returns>
		public static SpriteRenderer FlipX(this SpriteRenderer sr, bool? state = null)
		{
			if (!state.HasValue)
				state = !sr.flipX;

			sr.flipX = state.Value;
			return sr;
		}

		/// <summary>
		/// Fluent interface for setting the Y flip of this sprite renderer.
		/// </summary>
		/// <param name="sr"></param>
		/// <param name="state">The state of the Y flip. Left at null to toggle instead.</param>
		/// <returns></returns>
		public static SpriteRenderer FlipY(this SpriteRenderer sr, bool? state = null)
		{
			if (!state.HasValue)
				state = !sr.flipY;

			sr.flipY = state.Value;
			return sr;
		}

		public static Vector2 GetBinpackTrimOffsets(this Sprite sprite)
		{
			Vector2 trimOffset = Vector2.zero;

			if (sprite == null)
				return Vector2.zero;

			if (sprite.texture.name.Contains("binpack"))
			{
				trimOffset   =  sprite.pivot; // trimming stored in the pivot.
				trimOffset.x /= sprite.rect.width;
				trimOffset.y /= sprite.rect.height;
			}

			return trimOffset;
		}

        public static void SetWorldScale(this SpriteRenderer sprite, Vector2 worldUnits)
        {
            Vector2 units = sprite.sprite.size() / 32f;
            Vector2 scale = worldUnits    / units;

            sprite.transform.localScale = new Vector3(scale.x, scale.y, 1);
        }
	}
}