using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Util.Addressable {


	// Mainly copied/adapted from https://github.com/Unity-Technologies/Addressables-Sample/blob/master/Basic/ComponentReference/Assets/Samples/Addressables/1.19.19/ComponentReference/ComponentReference.cs

	[HideReferenceObjectPicker]
	public class ComponentRef<T> : AssetReference {

		public ComponentRef(string guid) : base(guid) { }


		public new AsyncOperationHandle<T> InstantiateAsync(Vector3 position, Quaternion rotation, Transform parent = null)
			=> Addressables.ResourceManager.CreateChainOperation(base.InstantiateAsync(position, rotation, parent), GameObjectReady);


		public new AsyncOperationHandle<T> InstantiateAsync(Transform parent = null, bool instantiateInWorldSpace = false)
			=> Addressables.ResourceManager.CreateChainOperation(base.InstantiateAsync(parent, instantiateInWorldSpace), GameObjectReady);

		public AsyncOperationHandle<T> LoadAssetAsync()
			=> Addressables.ResourceManager.CreateChainOperation(base.LoadAssetAsync<GameObject>(), GameObjectReady);

		AsyncOperationHandle<T> GameObjectReady(AsyncOperationHandle<GameObject> arg)
		{
			var comp = arg.Result.GetComponent<T>();
			return Addressables.ResourceManager.CreateCompletedOperation(comp, string.Empty);
		}

		public override bool ValidateAsset(Object obj)
		{
			var go = obj as GameObject;
			return go != null && go.GetComponent<T>() != null;
		}

		public override bool ValidateAsset(string path)
		{
			#if UNITY_EDITOR
			//this load can be expensive...
			var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
			return go != null && go.GetComponent<T>() != null;
			#else
            return false;
			#endif
		}

		public void ReleaseInstance(AsyncOperationHandle<T> op)
		{
			// Release the instance
			var component = op.Result as Component;
			if (component != null)
			{
				Addressables.ReleaseInstance(component.gameObject);
			}

			// Release the handle
			Addressables.Release(op);
		}
	}
}