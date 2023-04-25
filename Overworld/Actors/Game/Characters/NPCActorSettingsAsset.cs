using Sirenix.OdinInspector;
using Sirenix.Serialization;
using Util.Odin.Attributes;

namespace Anjin.Actors {
	public class NPCActorSettingsAsset : SerializedScriptableObject {

		[OdinSerialize, PropertyOrder(1), Inline]
		public NPCActor.Settings Settings = new NPCActor.Settings();

		public static implicit operator NPCActor.Settings(NPCActorSettingsAsset asset) => asset.Settings;
	}
}