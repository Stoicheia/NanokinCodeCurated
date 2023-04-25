using UnityEngine;

namespace Util
{
	public static class DefaultSpriteMaterial
	{
		public static Material New => new Material(Shader.Find("Sprites/Z Offset"));
	}
}

