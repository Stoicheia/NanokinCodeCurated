using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace Util.VFWAdditions
{
	[AttributeUsage(AttributeTargets.Enum | AttributeTargets.Field)]
	public class IconAttribute : Attribute
	{
		public readonly Texture2D tex;

		public IconAttribute(string path)
        {
#if UNITY_EDITOR
            tex = tex ?? EditorGUIUtility.FindTexture(path);
            tex = tex ?? AssetDatabase.LoadAssetAtPath<Texture2D>(path);
#endif
        }

		public IconAttribute(Texture2D tex)
		{
			this.tex = tex;
		}
	}
}

