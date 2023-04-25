using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Util.Extensions;
using Object = UnityEngine.Object;

namespace Util.Addressable
{

	[Serializable]
	public class Asset<T> where T: Object {

		public bool               IsDirect = false;
		public T                  DirectReference;
		public AssetReferenceT<T> AddressableReference;

		[NonSerialized]
		public T Loaded;

		public async UniTask<T> Load() => await _load();

		async UniTask<T> _load()
		{
			Loaded = null;
			if (IsDirect) {
				Loaded = DirectReference;
			} else if(AddressableReference != null && AddressableReference.IsSet())  {
				Loaded = await Addressables.LoadAssetAsync<T>(AddressableReference);
			}

			return Loaded;
		}

		public async void Test()
		{
			T a = await Load();

			T b = await this;
		}


		public static implicit operator T(Asset<T>      asset) => asset.Loaded;
		public static implicit operator Object(Asset<T> asset) => asset.Loaded;

		public static implicit operator bool(Asset<T> asset) => asset != null && asset.Loaded != null;
	}

	[Serializable]
	public class ComponentAsset<T> : Asset<GameObject> where T : Component {

		[NonSerialized]
		public T Component;


		public async UniTask<T> Load() => await _load();

		async UniTask<T> _load()
		{
			await base.Load();

			Component = null;

			if(Loaded != null) {
				Component = Loaded.GetComponent<T>();
				/*if (Component is IAsset asset)
					await asset.OnLoad();*/
			}

			return Component;
		}


		public static implicit operator T(ComponentAsset<T>          asset) => asset.Component;
		public static implicit operator GameObject(ComponentAsset<T> asset) => asset.Loaded;
		public static implicit operator Object(ComponentAsset<T>     asset) => asset.Loaded;

	}



	public static class AssetExtensions {

		/*public static UniTask.Awaiter GetAwaiter(this Asset asset) where T : Object
		{
			return asset.Load().GetAwaiter();
		} */

		public static UniTask<T>.Awaiter GetAwaiter<T>(this Asset<T>          asset) where T : Object    => asset.Load().GetAwaiter();
		public static UniTask<T>.Awaiter GetAwaiter<T>(this ComponentAsset<T> asset) where T : Component => asset.Load().GetAwaiter();

	}
}