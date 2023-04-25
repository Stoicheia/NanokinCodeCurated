using System;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Util.Odin.Attributes;

namespace Util.Assets
{
	[CreateNewWithSub]
	public abstract class ScriptableAsset : SerializedScriptableObject
	{ }


	[CreateNew]
	public abstract class ScriptableAsset<T> : SerializedScriptableObject {
		[OdinSerialize, PropertyOrder(1), Inline, VerticalGroup("Content")]
		protected T internalData;

		public T Value => internalData;

		public override string ToString()
		{
			return $"ScriptableAsset {{ {internalData.ToString()} }}";
		}

		private void TryInstantiate()
		{
			if (internalData == null && !typeof(T).IsAbstract)
			{
				internalData = Activator.CreateInstance<T>();
			}
		}
	}
}