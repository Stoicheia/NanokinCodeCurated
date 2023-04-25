using System.Linq;
using UnityEngine;

namespace Anjin.Util
{
	public static partial class Extensions
	{
		public static Vector2 BottomLeft(this   Rect rect) => new Vector2(rect.xMin, rect.yMin);
		public static Vector2 BottomRight(this  Rect rect) => new Vector2(rect.xMax, rect.yMin);
		public static Vector2 TopLeft(this      Rect rect) => new Vector2(rect.xMin, rect.yMax);
		public static Vector2 TopRight(this     Rect rect) => new Vector2(rect.xMax, rect.yMax);
		public static Vector2 TopCenter(this    Rect rect) => new Vector2(rect.center.x, rect.yMax);
		public static Vector2 BottomCenter(this Rect rect) => new Vector2(rect.center.x, rect.yMin);
		public static Vector2 MiddleRight(this  Rect rect) => new Vector2(rect.xMax, rect.center.y);
		public static Vector2 MiddleLeft(this   Rect rect) => new Vector2(rect.xMin, rect.center.y);

		public static Rect Move(this Rect rect, float x, float y) => rect.Move(new Vector2(x, y));
		public static Rect Move(this Rect rect, float offset) => rect.Move(Vector2.one * offset);

		public static Rect Move(this Rect rect, Vector2 offset)
		{
			Rect ret = new Rect(rect);
			ret.position += offset;
			return ret;
		}

		public static Rect MoveTopLeft(this Rect rect, float x, float y) => rect.MoveTopLeft(new Vector2(x, y));
		public static Rect MoveTopLeft(this Rect rect, float offset) => rect.MoveTopLeft(Vector2.one * offset);
		public static Rect MoveTop(this     Rect rect, float offset) => rect.MoveTopLeft(-Vector2.down * offset);
		public static Rect MoveLeft(this    Rect rect, float offset) => rect.MoveTopLeft(Vector2.right * offset);

		public static Rect MoveTopLeft(this Rect rect, Vector2 offset)
		{
			Rect ret = new Rect(rect);
			ret.min += offset;
			return ret;
		}

		public static Rect MoveBottomRight(this Rect rect, float x, float y) => rect.MoveBottomRight(new Vector2(x, y));
		public static Rect MoveBottomRight(this Rect rect, float offset) => rect.MoveBottomRight(Vector2.one * offset);
		public static Rect MoveBottom(this      Rect rect, float offset) => rect.MoveBottomRight(-Vector2.down * offset);
		public static Rect MoveRight(this       Rect rect, float offset) => rect.MoveBottomRight(Vector2.right * offset);

		public static Rect MoveBottomRight(this Rect rect, Vector2 offset)
		{
			Rect ret = new Rect(rect);
			ret.max += offset;
			return ret;
		}

		public static Rect SizeUp(this Rect rect, float width, float height) => rect.Move(new Vector2(width, height));
		public static Rect SizeUp(this Rect rect, float addsize) => rect.Move(Vector2.one * addsize);

		public static Rect SizeUp(this Rect rect, Vector2 addSize)
		{
			Rect ret = new Rect(rect);
			ret.size += addSize;
			return ret;
		}

		public static Rect Resize(this Rect rect, float?  width, float? height) => rect.Resize(new Vector2(width ?? rect.width, height ?? rect.height));
		public static Rect Resize(this Rect rect, Vector2 size) => new Rect(rect) {size = size};

		public static Rect Downsize(this Rect rect, float w, float h)
		{
			float w2 = w / 2f;
			float h2 = h / 2f;

			rect.MoveTopLeft(rect.center.x - w2, rect.center.y - h2);
			rect.MoveBottomRight(rect.center.x + w2, rect.center.y + h2);

			return rect;
		}

		public static Rect Scaled(this Rect rect, float s) => new Rect(
			rect.x * s,
			rect.y * s,
			rect.width * s,
			rect.height * s
		);

		public static Rect Inset(this Rect rect, float value) => Outset(rect, -value, -value, -value, -value);

		/// <summary>
		/// Inset operation.
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="left"></param>
		/// <param name="top"></param>
		/// <param name="right"></param>
		/// <param name="down"></param>
		/// <returns></returns>
		public static Rect Inset(this Rect rect, float left, float top, float right, float down) => Outset(rect, -left, -top, -right, -down);

		/// <summary>
		/// Outset operation by a fixed amount on all sides.
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="s"></param>
		/// <returns>A new rectangle based on the passed rectangle, with the operation applied on it.</returns>
		public static Rect Outset(this Rect rect, float s) => Outset(rect, s, s, s, s);

		/// <summary>
		/// Outset operation. (Extends the rectangle outward)
		/// </summary>
		/// <param name="rect"></param>
		/// <param name="left">How many units to extend to the left.</param>
		/// <param name="top">How many units to extend to the top.</param>
		/// <param name="right">How many units to extend to the right.</param>
		/// <param name="down">How many units to extend to the bottom.</param>
		/// <returns>A new rectangle based on the passed rectangle, with the operation applied on it.</returns>
		public static Rect Outset(this Rect rect, float left, float top, float right, float down) =>
			new Rect
			{
				xMin = rect.xMin - left,
				yMin = rect.yMin - top,
				xMax = rect.xMax + right,
				yMax = rect.yMax + down
			};

		/// <summary>
		/// Union operation between several rectangles.
		/// </summary>
		/// <param name="rectangles"></param>
		/// <returns>A new rectangle which encompasses ALL rectangles.</returns>
		public static Rect Union(this Rect[] rectangles)
		{
			switch (rectangles.Length)
			{
				case 0:
					return Rect.zero;

				case 1:
					return rectangles[0];
			}

			Rect[] others = rectangles.Skip(1).ToArray();
			return rectangles[0].Union(others);
		}

		/// <summary>
		/// Union operation between two rectangles.
		/// </summary>
		/// <param name="rect1">The first rectangle.</param>
		/// <param name="rect2">The second rectangle.</param>
		/// <returns>A new rectangle which encompasses rect1 and rect2.</returns>
		public static Rect Union(this Rect rect1, Rect rect2)
		{
			float xMin = Mathf.Min(rect1.xMin, rect2.xMin);
			float yMin = Mathf.Min(rect1.yMin, rect2.yMin);
			float xMax = Mathf.Max(rect1.xMax, rect2.xMax);
			float yMax = Mathf.Max(rect1.yMax, rect2.yMax);

			Rect rect = new Rect
			{
				xMin = xMin,
				yMin = yMin,
				xMax = xMax,
				yMax = yMax
			};

			return rect;
		}

		/// <summary>
		/// Union operation between a base rectangle and several other rectangles.
		/// </summary>
		/// <param name="rect">The base rectangle.</param>
		/// <param name="rectangles"></param>
		/// <returns>A new rectangle which encompasses ALL rectangles.</returns>
		public static Rect Union(this Rect rect, Rect[] rectangles)
		{
			Rect union = rect;

			foreach (Rect t in rectangles)
				union = union.Union(t);

			return union;
		}

		public static Rect Floor(this Rect rect, float snap = 1)
		{
			rect.position = rect.position.Floor(snap);
			rect.size     = rect.size.Floor(snap);
			return rect;
		}

		public static Rect Ceil(this Rect rect, float snap = 1)
		{
			rect.position = rect.position.Ceil(snap);
			rect.size     = rect.size.Ceil(snap);
			return rect;
		}
	}
}