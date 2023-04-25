using System.Collections.Generic;
using Anjin.Scripting;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using MoonSharp.Interpreter;
using Overworld.Tags;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Overworld.Cutscenes
{
	/// <summary>
	/// A base for objects that can be directed by a coroutine player.
	/// </summary>
	[LuaUserdata(Descendants = true)]
	public abstract class DirectedBase
	{
		public    GameObject gameObject;
		protected GameObject loadedPrefab;

		// Options
		public Table    options;
		public string   address;
		public string   name;
		public bool     forceSpawn;
		public bool     keepAfter;
		public bool     controlFlag = true; // ONLY means that the actor will be controlled OnStart.
		public DynValue initialPosition;
		public DynValue initialTarget;
		public bool     walkToPosition;
		public bool     spawnDistributed;

		public bool     reset_transform = false;

		// Execution flags
		public bool loaded;
		public bool started;
		public bool alreadyExisted;
		public bool controlling;

		// Tags
		public bool         tagsApplied;
		public bool         hadTags;
		public bool         removeBaseTags;
		public List<string> removedTags;

		protected DirectedBase(Table options)
		{
			name        = null;
			removedTags = new List<string>();

			this.options = options;

			address = null;

			ReadOptions(options);
		}

		public virtual void ReadOptions(Table options)
		{
			if (options == null)
				return;

			options.TryGet("name",              out name,            name);
			options.TryGet("keep",              out keepAfter,       keepAfter);
			options.TryGet("asset",             out address,         address);
			options.TryGet("controls",          out controlFlag,     controlFlag);
			options.TryGet("force_spawn",       out forceSpawn,      forceSpawn);
			options.TryGet("pos",               out initialPosition, initialPosition);
			options.TryGet("spawn_distributed", out initialPosition, initialPosition);
			options.TryGet("remove_base_tags",  out removeBaseTags,  removeBaseTags);
			options.TryGet("reset_transform",   out reset_transform, reset_transform);
		}

		/// <summary>
		/// Load the actor, if necessary.
		/// </summary>
		/// <returns></returns>
		public virtual async UniTask Load()
		{
			if (loaded)
				return;

			// TODO move this to Cutscene.Load and manage the handles correctly (release the handle OnDestroy or OnDisable)
			if (address != null)
			{
				loadedPrefab = await Addressables.LoadAssetAsync<GameObject>(address).Task;
			}

			loaded = true;
		}

		/// <summary>
		/// Start the directed so it can be used by the coplayer.
		/// </summary>
		public virtual void OnStart(Coplayer coplayer, bool auto_spawn = true)
		{
			if (started)
				return;

			started = true;

			// Uses existing gameObject passed in directly thru the constructor
			alreadyExisted = gameObject != null;

			ApplyCoplayerTags(coplayer);
		}

		/// <summary>
		/// Stop this directed.
		/// </summary>
		public virtual void OnStop(Coplayer coplayer)
		{
			started = false;
			if (gameObject && !alreadyExisted && !keepAfter)
			{
				Release();
			}

			RemoveCoplayerTags(coplayer);
		}

		public virtual void Update() { }

		public virtual void Release() { }

		protected void ApplyCoplayerTags(Coplayer coplayer)
		{
			if (!gameObject || tagsApplied) return;

			var tags = TagController.EnsureTaggable(gameObject, out hadTags);

			if (hadTags && removeBaseTags)
			{
				removedTags.AddRange(tags.Tags);
				tags.RemoveAll();
			}

			for (int i = 0; i < coplayer.memberTags.Count; i++)
			{
				tags.AddTag(coplayer.memberTags[i]);
				tagsApplied = true;
			}

			if(coplayer.Logging)
				Debug.Log($"[TRACE] Applied coplayer tags to {gameObject}.", gameObject);
		}

		protected void RemoveCoplayerTags(Coplayer coplayer)
		{
			if (!gameObject || !tagsApplied) return;

			if (!hadTags)
				gameObject.RemoveComponent<Tag>();
			else
			{
				if (gameObject.TryGetComponent(out Tag tags))
				{
					if (hadTags && removeBaseTags)
					{
						for (int i = 0; i < removedTags.Count; i++)
						{
							tags.AddTag(removedTags[i]);
						}

						removedTags.Clear();
					}

					for (int i = 0; i < coplayer.memberTags.Count; i++)
					{
						tags.RemoveTag(coplayer.memberTags[i]);
					}
				}
			}

			if(coplayer.Logging)
				Debug.Log($"[TRACE] Removed coplayer tags from {gameObject}.", gameObject);
		}
	}
}