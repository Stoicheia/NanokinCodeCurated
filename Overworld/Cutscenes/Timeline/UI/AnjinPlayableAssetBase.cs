using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Playables;

namespace Overworld.Cutscenes.Timeline.UI {

	[ShowOdinSerializedPropertiesInInspector]
	[HideMonoScript]
	public abstract class AnjinPlayableAssetBase : PlayableAsset, ISerializationCallbackReceiver {
		[HideInInspector] public double clipStart;

		[SerializeField]
		[HideInInspector]
		private SerializationData serializationData;

		void ISerializationCallbackReceiver.OnAfterDeserialize()
		{
			UnitySerializationUtility.DeserializeUnityObject(this, ref serializationData);
			OnAfterDeserialize();
		}

		void ISerializationCallbackReceiver.OnBeforeSerialize()
		{
			OnBeforeSerialize();
			UnitySerializationUtility.SerializeUnityObject(this, ref serializationData);
		}

		/// <summary>Invoked after deserialization has taken place.</summary>
		protected virtual void OnAfterDeserialize() { }
		/// <summary>Invoked before serialization has taken place.</summary>
		protected virtual void OnBeforeSerialize() { }

		#if UNITY_EDITOR
		[HideInTables]
		[OnInspectorGUI]
		[PropertyOrder(-2.147484E+09f)]
		private void InternalOnInspectorGUI() => EditorOnlyModeConfigUtility.InternalOnInspectorGUI((Object) this);
		#endif
	}
}