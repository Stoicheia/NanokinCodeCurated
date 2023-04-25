using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Util.Addressable
{
	public class AsyncHandles
	{
		public List<AsyncOperationHandle> list = new List<AsyncOperationHandle>();

		public void Add(AsyncOperationHandle hnd)
		{
			list.Add(hnd);
		}

		public void ReleaseAll()
		{
			foreach (AsyncOperationHandle handle in list)
			{
				Addressables.Release(handle);
			}

			list.Clear();
		}

		public Task WhenAll()
		{
			return Task.WhenAll(list.Where(h => !h.IsDone).Select(h => h.Task));
		}

		public async UniTask<TAsset> LoadAssetAsyncSafe<TAsset>(string address)
			where TAsset : class
		{
			try
			{
				AsyncOperationHandle<TAsset> handle = await Addressables2.LoadHandleAsync<TAsset>(address);
				list.Add(handle);
				return handle.Result;
			}
			catch (InvalidKeyException e)
			{
				DebugLogger.LogException(e);
			}

			return null;
		}


		public async UniTask<TAsset> LoadAssetAsync<TAsset>(string address)
		{
			var handle = await Addressables2.LoadHandleAsync<TAsset>(address);
			list.Add(handle);
			return handle.Result;
		}

		public async UniTask<TAsset> LoadAssetAsync<TAsset>(AssetReferenceT<TAsset> aref)
			where TAsset : Object
		{
			var handle = await Addressables2.LoadHandleAsync<TAsset>(aref);
			list.Add(handle);
			return handle.Result;
		}
	}
}