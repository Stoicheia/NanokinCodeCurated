using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Util;
using UnityEngine.SceneManagement;
using Vexe.Runtime.Extensions;

namespace Anjin.Utils
{
	public class MultiSceneLoading : AsyncSceneOperation
	{
		private List<AsyncSceneOperation> _waitList;
		private List<AsyncSceneOperation> _all;

		public MultiSceneLoading(IEnumerable<AsyncSceneOperation> operations)
		{
			_waitList = operations.ToList();
			_all      = operations.ToList();

			foreach (AsyncSceneOperation asyncOperation in _waitList)
			{
				asyncOperation.Complete += scene =>
				{
					_waitList.Remove(asyncOperation);

					if (_waitList.IsEmpty())
					{
						// All operations done!
						IsDone = true;
						OnComplete(new Scene());
					}
				};
			}
		}

		public MultiSceneLoading(params AsyncSceneOperation[] sceneOperations) : this((IEnumerable<AsyncSceneOperation>) sceneOperations)
		{ }

		public override AsyncSceneOperation OnDriver<TDriver>(Action<TDriver> callback)
		{
			Complete += scene =>
			{
				// Iterate every operation which loaded a scene to find the driver we're looking for.
				foreach (AsyncSceneOperation awaitedOp in _all)
				{
					if (awaitedOp.LoadedScene.IsValid())
					{
						TDriver driver = awaitedOp.LoadedScene.FindRootComponent<TDriver>();
						callback?.Invoke(driver);
					}
				}
			};

			return this;
		}
	}
}