using System;
using System.Collections.Generic;
using Anjin.Util;
using UnityEngine;

namespace UnityEditor.AI.Anjin
{
	public class SpriteTrimmingCache
	{
		// TODO this could lead to memory leaks in the future if sprites are deloaded at runtime and stuff. We will review the usage of this class once we know how we handle loading in the game.

		private static Dictionary<Sprite, Rect> _bounds = new Dictionary<Sprite, Rect>();

		/// <summary>
		/// Returns the bounds of the sprite by discarding transparent pixels.
		/// </summary>
		/// <param name="sprite"></param>
		/// <returns></returns>
		public static Rect GetTrimmedBounds(Sprite sprite)
		{
			// if (!sprite)
			// {
			// 	return Rect.zero;
			// }
			//
			// return sprite.rect;
			//
			if (!sprite.texture.isReadable)
			{
				Debug.Log($"Note: cannot trim bounds of sprites '{sprite.name}' because Read/Write is not enabled on it.");
				return _bounds[sprite] = new Rect(Vector2.zero, sprite.size());
			}


			// Temporary removal for testing

			if (_bounds.TryGetValue(sprite, out Rect bounds))
				return bounds;

			Rect    spriteRect = sprite.rect;
			Vector2 spriteSize = sprite.size();

			Color[] pixels = sprite.texture.GetPixels(
				(int) spriteRect.x,
				(int) spriteRect.y,
				(int) spriteRect.width,
				(int) spriteRect.height
			); // pixels left to right, bottom to top (row after row)

			// not gonna lie I bruteforced these a b parameters
			Vector2 ul = new Vector2(
				SweepLeft(pixels, spriteSize),
				SweepTop(pixels, spriteSize)
			);

			Vector2 dr = new Vector2(
				SweepRight(pixels, spriteSize),
				SweepBottom(pixels, spriteSize)
			);



			float left   = ul.x;
			float right  = dr.x;
			float top    = ul.y;
			float bottom = dr.y;

			// Back to GL inverted vertical axis.
			// top    = (int) (spriteSize.y - top    - 1);
			// bottom = (int) (spriteSize.y - bottom - 1);

			// Offset to the sprite's base position.
			// left   += (int) spriteRect.x;
			// right  += (int) spriteRect.x;
			// top    += (int) spriteRect.y;
			// bottom += (int) spriteRect.y;

			float w = (right - left).Abs() + 1;
			float h = (top - bottom).Abs() + 1;

			bounds = new Rect(
				left,
				top,
				w,
				h
			);

			return _bounds[sprite] = bounds;
		}

		private static float SweepLeft(Color[] pixels, Vector2 size)
		{
			for (int x = 0; x < size.x; x++)
			for (int y = 0; y < size.y; y++)
			{
				Color pixel = GetPixel(pixels, x, y, size);
				if (pixel.a > Mathf.Epsilon) return x;
			}

			throw new InvalidOperationException("The swept sprite is all transparency.");
		}

		private static float SweepTop(Color[] pixels, Vector2 size)
		{
			for (int y = 0; y < size.y; y++)
			for (int x = 0; x < size.x; x++)
			{
				Color pixel = GetPixel(pixels, x, y, size);
				if (pixel.a > Mathf.Epsilon) return y;
			}

			throw new InvalidOperationException("The swept sprite is all transparency.");
		}

		private static float SweepRight(Color[] pixels, Vector2 size)
		{
			for (int x = (int) (size.x - 1); x >= 0; x--)
			for (int y = 0; y < size.y; y++)
			{
				Color pixel = GetPixel(pixels, x, y, size);
				if (pixel.a > Mathf.Epsilon) return x;
			}

			throw new InvalidOperationException("The swept sprite is all transparency.");
		}

		private static float SweepBottom(Color[] pixels, Vector2 size)
		{
			for (int y = (int) (size.y - 1); y >= 0; y--)
			for (int x = 0; x < size.x; x++)
			{
				Color pixel = GetPixel(pixels, x, y, size);
				if (pixel.a > Mathf.Epsilon) return y;
			}

			throw new InvalidOperationException("The swept sprite is all transparency.");
		}

		private static Color GetPixel(Color[] pixels, int x, int y, Vector2 size)
		{
			y = (int) size.y - y - 1;
			return pixels[y * (int) size.x + x];
		}
	}
}