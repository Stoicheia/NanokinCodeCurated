using System;
using System.Collections.Generic;
using Assets;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;
using Util.Addressable;

namespace SaveFiles
{
	/// <summary>
	/// Defines the progress of one quest in the game.
	/// Actual information for the quest is stored eleswhere with the game files. (QuestAsset)
	/// Having the entry for a quest makes it appear into the quest tracker.
	/// </summary>
	[Serializable]
	public class QuestEntry
	{
		public string          Address;
		public bool            Completed;
		public List<Objective> Objectives = new List<Objective>();

		public async UniTask<Mirror> ToMirrorAsync()
		{
			var handle = await Addressables2.LoadHandleAsync<QuestAsset>($"Quest/{Address}");
			return new Mirror(this, handle.Result, handle);
		}

		/// <summary>
		/// quick struct for future proofing
		/// (in case we need to store more than just progress)
		/// </summary>
		public struct Objective
		{
			public bool Completed;
			public int  Progress;
		}

		public readonly struct Mirror
		{
			public readonly QuestEntry                       entry;
			public readonly QuestAsset                       asset;
			public readonly AsyncOperationHandle<QuestAsset> handle;

			public Mirror(QuestEntry entry, QuestAsset asset, AsyncOperationHandle<QuestAsset> handle)
			{
				this.entry  = entry;
				this.asset  = asset;
				this.handle = handle;
			}
		}
	}
}