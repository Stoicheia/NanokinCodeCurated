using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Anjin.Scripting;
using Anjin.Util;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Combat.Scripting
{
	/// <summary>
	/// Utility class to watch for lua file changes.
	/// Allows making aspects of the game react instantly to lua files
	/// changing without needing an AssetDatabase refresh. (CTRL-R)
	///
	/// In builds, all functions are stubbed out.
	/// </summary>
	public static class LuaChangeWatcher
	{
		private static FileSystemWatcher                       _watcher;
		private static Dictionary<object, List<ChangeWatcher>> _ownerWatches         = new Dictionary<object, List<ChangeWatcher>>();
		private static Dictionary<string, List<ChangeWatcher>> _fileWatches          = new Dictionary<string, List<ChangeWatcher>>();
		private static List<ChangedFile>                       _bufferedChanges      = new List<ChangedFile>();
		private static List<object>                            _collectedFiles       = new List<object>();
		private static Dictionary<string, DateTime>            _lastChangeEventTimes = new Dictionary<string, DateTime>();
		private static object                                  _threadLock           = new object();

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
		private static void InitializeLazily()
		{
			// Static constructor was unreliable, so we use this instead to initialize right
			// when we need to access the watcher!

			if (_watcher != null) return;
			Initialize();
		}

		private static void Initialize()
		{
#if UNITY_EDITOR
			// This appears to take a while (~1-2 second)
			// Not too big of a deal because this is cached
			// just once with playmode domain reload turned off.
			string dataPath = Application.dataPath;

			Task.Run(() =>
			{
				_watcher = new FileSystemWatcher
				{
					Path                  = dataPath,
					NotifyFilter          = NotifyFilters.LastWrite,
					Filter                = "*.lua",
					IncludeSubdirectories = true,
					EnableRaisingEvents   = true,
				};

				_watcher.Changed += OnFileChanged;
			});

#endif
		}

		private static void OnFileChanged(object sender, [NotNull] FileSystemEventArgs args)
		{
#if UNITY_EDITOR
			if (args.ChangeType != WatcherChangeTypes.Changed)
				return;

			// https://stackoverflow.com/a/450046/1319727
			lock (_threadLock)
			{
				if (_lastChangeEventTimes.TryGetValue(args.Name, out DateTime lastChange) && DateTime.Now.Subtract(lastChange).TotalMilliseconds < 500)
					return;

				_lastChangeEventTimes[args.Name] = DateTime.Now;
			}

			string name     = Path.GetFileNameWithoutExtension(args.Name);
			string fullpath = args.FullPath;

			Lua.envScript?.ClearSourceCache(name);
			Lua.envScript?.ClearSourceCache(fullpath);

			if (_fileWatches.TryGetValue(name, out List<ChangeWatcher> watches))
			{
				DebugLogger.Log($"[LUA WATCH] Detected script change for {name}. Refreshing...", LogContext.Combat, LogPriority.Low);

				// We don't have the lua asset at this time, so we need
				// to buffer the change and manually flush it later to the right asset, with FLushChanges(asset).
				// For require directives (accessing scripts by name only) our script loaders take care of this automatically.
				// For direct reference of lua assets in components, they must be flushed manually through the watch's onChange.
				_bufferedChanges.Add(new ChangedFile(fullpath));

				foreach (ChangeWatcher watch in watches)
				{
					// FileChangeWatcher runs on its own thread and this is invoked from that thread. (unity is not thread safe, most functions don't work outside the main thread)
					// so this is just a cute little hack really
					UniTask.Create(async () =>
					{
						Lua.envScript?.ClearSourceCache(watch.name);
						await UniTask.SwitchToMainThread();
						watch.onChange();
					});
				}
			}
			else
			{
				UniTask.Create(async () =>
				{
					await UniTask.SwitchToMainThread();

					string   path  = $"Assets{PathUtil.GetRelativePath(Application.dataPath, fullpath)}";
					LuaAsset asset = AssetDatabase.LoadAssetAtPath<LuaAsset>(path);

					if (asset != null && File.Exists(fullpath))
						asset.UpdateText(File.ReadAllText(fullpath));
				});
			}
#endif
		}

		[Conditional("UNITY_EDITOR")]
		public static void ClearWatches([NotNull] object owner)
		{
#if UNITY_EDITOR
			InitializeLazily();

			if (!_ownerWatches.TryGetValue(owner, out List<ChangeWatcher> ownerWatches))
				return;

			_ownerWatches.Remove(owner);

			foreach (ChangeWatcher watch in ownerWatches)
				if (_fileWatches.TryGetValue(watch.name, out List<ChangeWatcher> fileWatches))
					fileWatches.Remove(watch); // O(n) but that's ok for our purposes, should never be a concern
#endif
		}

		[Conditional("UNITY_EDITOR")]
		public static void Watch([NotNull] object owner, string name, [CanBeNull] string path, Action onChange)
		{
#if UNITY_EDITOR
			InitializeLazily();

			name = Path.GetFileNameWithoutExtension(name);
			var watch = new ChangeWatcher(name, path, onChange);

			List<ChangeWatcher> ownerWatches = _ownerWatches.GetOrCreate(owner);
			List<ChangeWatcher> fileWatches  = _fileWatches.GetOrCreate(path != null ? Path.GetFileNameWithoutExtension(path) : name);

			// Owner is already watching this file
			if (ownerWatches.Any(se => se.name == name))
				return;

			ownerWatches.Add(watch);
			fileWatches.Add(watch);
#endif
		}

		[Conditional("UNITY_EDITOR")]
		public static void Watch([NotNull] object owner, string name, Action onChange)
		{
#if UNITY_EDITOR
			Watch(owner, name, null, onChange);
#endif
		}

		[Conditional("UNITY_EDITOR")]
		public static void Watch([NotNull] object owner, [NotNull] LuaAsset asset, Action onChange)
		{
#if UNITY_EDITOR
			Watch(owner, asset.name, asset.Path, onChange);
#endif
		}

		/// <summary>
		/// Starts collecting used lua files.
		/// </summary>
		[Conditional("UNITY_EDITOR")]
		public static void BeginCollecting()
		{
			_collectedFiles.Clear();
		}

		public static void Use(string name)
		{
			_collectedFiles.Add(name);
		}

		public static void Use(LuaAsset asset)
		{
			_collectedFiles.Add(asset);
		}

		/// <summary>
		/// Stop the current collecting and create
		/// watches for them which are attached to 'owner'.
		/// </summary>
		/// <param name="owner">Owner of the collected files that will be watched.</param>
		/// <param name="onChangeNotified">Handler for the watches detecting a file change.</param>
		[Conditional("UNITY_EDITOR")]
		public static void EndCollecting(object owner, Action onChangeNotified)
		{
			foreach (object queuedWatch in _collectedFiles)
				switch (queuedWatch)
				{
					case string file:
						Watch(owner, file, null, onChangeNotified);
						break;

					case LuaAsset asset:
						Watch(owner, asset, onChangeNotified);
						break;
				}

			_collectedFiles.Clear();
		}

		/// <summary>
		/// Flush all changed lua scripts.
		/// </summary>
		public static void FlushChanges()
		{
#if UNITY_EDITOR
			foreach (ChangedFile change in _bufferedChanges)
			{
				string   path  = PathUtil.GetRelativePath(Application.dataPath, change.fullPath);
				LuaAsset asset = AssetDatabase.LoadAssetAtPath<LuaAsset>(path);

				if (asset != null)
					asset.UpdateText(File.ReadAllText(change.fullPath));
			}
#endif
		}

		/// <summary>
		/// Flush any queued detected changes for the given LuaAsset.
		/// </summary>
		/// <param name="asset"></param>
		/// <returns>Whether or not there were new changes.</returns>
		[Conditional("UNITY_EDITOR")]
		public static void FlushChanges(LuaAsset asset)
		{
#if UNITY_EDITOR
			InitializeLazily();

			if (_bufferedChanges.Count == 0)
				return;

			string token = Path.GetFileNameWithoutExtension(asset.Path);

			for (var i = 0; i < _bufferedChanges.Count; i++)
			{
				ChangedFile change = _bufferedChanges[i];

				if (change.fullPath == null || token == null) continue; // apparently that can happen?
				if (Path.GetFileNameWithoutExtension(change.fullPath) == token)
				{
					asset.UpdateText(File.ReadAllText(change.fullPath));
					_bufferedChanges.RemoveAt(i);
					return;
				}
			}
#endif
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			_ownerWatches.Clear();
			_fileWatches.Clear();
		}

		/// <summary>
		/// A watch for changes matching a logical name.
		/// </summary>
		public readonly struct ChangeWatcher
		{
			public readonly string name; // no path and no extension, e.g. 'cutscene_header'
			public readonly string path;
			public readonly Action onChange;

			public ChangeWatcher(string name, string path, Action onChange)
			{
				this.name     = name;
				this.path     = path;
				this.onChange = onChange;
			}

			public bool Equals(ChangeWatcher other) => name == other.name && Equals(onChange, other.onChange);

			public override bool Equals(object obj) => obj is ChangeWatcher other && Equals(other);

			public override int GetHashCode()
			{
				unchecked
				{
					return ((name != null ? name.GetHashCode() : 0) * 397) ^ (onChange != null ? onChange.GetHashCode() : 0);
				}
			}
		}

		/// <summary>
		/// A file that has been changed.
		/// </summary>
		public readonly struct ChangedFile
		{
			public readonly string fullPath;

			public ChangedFile(string fullPath)
			{
				this.fullPath = fullPath;
			}
		}

		public enum HotReloadBehaviors
		{
			ContinueExisting,
			Stop,
			Replay,
			WaitForEnd
		}
	}
}