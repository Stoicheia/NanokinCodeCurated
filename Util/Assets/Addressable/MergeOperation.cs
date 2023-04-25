using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Util.Addressable
{
	public class MergeOperation<T> : AsyncOperationBase<T>
	{
		private readonly string[]                       keys = new string[2];
		private          AsyncOperationHandle<IList<T>> handle;

		public MergeOperation(string address, string label)
		{
			keys[0] = address;
			keys[1] = label;
		}

		protected override void Execute()
		{
			handle = Addressables.LoadAssetsAsync<T>(keys, null, Addressables.MergeMode.Intersection);
			handle.Completed += handle =>
			{
				var result = handle.Result;

				if (result.Count > 0)
					base.Complete(handle.Result[0], true, string.Empty);

				else if (result.Count == 0 || result.Count > 1)
					Debug.LogError("Inconclusive assets loaded for address: " + keys[0]);
			};
		}

		protected override void Destroy()
		{
			base.Destroy();

			if (handle.IsValid())
				Addressables.Release(handle);
		}
	}
}