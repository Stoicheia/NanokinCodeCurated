using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Util.Odin.Attributes;

namespace Util.Assets
{
	[Serializable, Inline]
	public class Asset<TData, TScriptableObject>
		where TScriptableObject : ScriptableAsset<TData>
	{
		public enum Sources
		{
			Internal,
			External
		}

		[HideLabel, ShowIf("IsExternal")]
		public TScriptableObject externalValue;

		[HideInInspector]
		public Sources source = Sources.External;

		[Inline,
		 OdinSerialize,
		 ShowIf("@!IsExternal")]
		public TData inbuiltValue;

		public bool IsExternal => source == Sources.External;

		public TData Value =>
			IsExternal && externalValue != null
				? externalValue.Value
				: inbuiltValue;

		public Type ScriptableType => typeof(TScriptableObject);

		public static implicit operator TData(Asset<TData, TScriptableObject> asset)
		{
			return asset.Value;
		}

		public void Set(TScriptableObject value)
		{
			source        = Sources.External;
			externalValue = value;
		}

		public void Set(TData data)
		{
			source       = Sources.Internal;
			inbuiltValue = data;
		}
	}
}