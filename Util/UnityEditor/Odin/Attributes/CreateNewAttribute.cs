using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Util.Odin.Attributes
{
	[AttributeUsage(AttributeTargets.All)]
	public class CreateNewAttribute : Attribute
	{
		public readonly string baseAsset;
		public readonly string memberSelectorEntries;
		public readonly Type   type;

		public CreateNewAttribute(string baseAsset = null, string memberSelectorEntries = null, Type type = null)
		{
			this.baseAsset             = baseAsset;
			this.memberSelectorEntries = memberSelectorEntries;
			this.type                  = type;
		}

		public CreateNewAttribute(ScriptableObject baseAsset, string memberSelectorEntries = null, Type type = null)
		{
			#if UNITY_EDITOR
			string assetPath = AssetDatabase.GetAssetPath(baseAsset);
			string basePath  = $"{Path.GetDirectoryName(assetPath)}\\";
			this.baseAsset             = basePath;
			#endif

			this.memberSelectorEntries = memberSelectorEntries;
			this.type                  = type;
		}
	}
}