
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Util.Odin.Attributes;

namespace Util.ConfigAsset
{

	public abstract class SettingsAsset<T> : SerializedScriptableObject where T: new()
	{
		[OdinSerialize, PropertyOrder(1), Inline]
		public T Settings = new T();

		public static implicit operator T(SettingsAsset<T> asset) => asset.Settings;
	}
}