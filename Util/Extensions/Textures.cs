using UnityEngine;

namespace Anjin.Util
{
	public static partial class Extensions
	{
		/// <summary>
		/// Get the size of the texture
		/// </summary>
		/// <param name="tex"></param>
		/// <returns></returns>
		public static Vector2 size(this Texture2D tex) => new Vector2(tex.width, tex.height);
	}


	public static partial class Extensions
	{
		 public static void Clear(this RenderTexture rt, Color color)
		 {
		 	RenderTexture.active = rt;
		 	GL.Clear(false, true, color);
		 	RenderTexture.active = null;
		 }

		public static void Blit(this RenderTexture rt, Material material)
		{
			Graphics.Blit(rt, material);
		}

		public static void Blit(this RenderTexture rt, RenderTexture other)
		{
			Graphics.Blit(rt, other);
		}

		public static void Blit(this RenderTexture rt, RenderTexture other, Material material)
		{
			Graphics.Blit(rt, other, material);
		}
	}
}