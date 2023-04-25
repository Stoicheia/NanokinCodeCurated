using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Anjin.Utils;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Util.Addressable
{
	public interface IAddressable
	{
		string Address { get; set; }
	}

	public class AsyncLoading
	{
		private HashSet<AsyncOperationHandle>            _addressableOperations = new HashSet<AsyncOperationHandle>();
		private HashSet<AsyncSceneOperation>             _sceneOperations       = new HashSet<AsyncSceneOperation>();
		private Dictionary<AsyncOperationHandle, string> _handleAddresses       = new Dictionary<AsyncOperationHandle, string>();
		private List<Task>                               _tasks                 = new List<Task>();

		private bool _hasCompleted;

		public bool AllDone => _addressableOperations.Count == 0
		                       && _sceneOperations.Count == 0;

		public event Action Completed;

		public Task NewTask => Task.WhenAll(_tasks);

		public AsyncOperationHandle<TType> Add<TType>(AsyncOperationHandle<TType> handle)
		{
			_hasCompleted = false;

			_addressableOperations.Add(handle);
			_tasks.Add(handle.Task);

			handle.Completed += hnd =>
			{
				if (hnd.Result is IAddressable addressable && _handleAddresses.TryGetValue(hnd, out string address))
				{
					_handleAddresses.Remove(hnd);
					addressable.Address = address;
				}

				_addressableOperations.Remove(hnd);
				CheckForCompletion();
			};


			return handle;
		}

		public void Add(AsyncSceneOperation op)
		{
			if (op.IsDone)
				return;

			_sceneOperations.Add(op);
			op.Complete += scene =>
			{
				_sceneOperations.Remove(op);
				CheckForCompletion();
			};
		}

		public AsyncOperationHandle<TAsset> Add<TAsset>(string address)
			where TAsset:Object
		{
			AsyncOperationHandle<TAsset> handle = Addressables.LoadAssetAsync<TAsset>(address);
			_handleAddresses[handle] = address;
			Add(handle);
			return handle;
		}

		public AsyncOperationHandle<TType> Add<TType>(AssetReferenceT<TType> assetRef)
			where TType : Object
		{
			AsyncOperationHandle<TType> handle = assetRef.LoadAssetAsync();
			_handleAddresses[handle] = (string) assetRef.RuntimeKey;
			Add(handle);
			return handle;
		}

		public void CheckForCompletion()
		{
			if (_hasCompleted) return;

			if (_addressableOperations.Count == 0 && _sceneOperations.Count == 0)
			{
				_hasCompleted = true;
				Completed?.Invoke();
			}
		}
	}
}