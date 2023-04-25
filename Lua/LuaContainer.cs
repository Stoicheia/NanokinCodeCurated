using System;
using System.Collections.Generic;
using Anjin.Nanokin;
using Combat.Scripting;
using Cysharp.Threading.Tasks;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.Scripting
{
	// Todo(C.L.): Safety checks
	[LuaUserdata]
	public abstract class LuaContainerBase
	{
		public bool RequiresPlayer = false;

		[NonSerialized, ShowInPlay]
		public RuntimeState _state;

		private Action<LuaContainerBase> onInit;

		bool _scheduledReload;

		protected abstract string ScriptName { get; }
		protected abstract void   LoadScript(bool editor_reload);

		public LuaContainerBase() : this(false) { }

		public LuaContainerBase(bool requiresPlayer)
		{
			RequiresPlayer = requiresPlayer;
		}

		public void OnStart(Component parent, Action<LuaContainerBase> onInit)
		{
			this.onInit = onInit;
			OnStart(parent);
		}

		public async void OnStart(Component parent, params string[] dependancies)
		{
			_scheduledReload = false;

			_state = new RuntimeState
			{
				parent       = parent,
				dependancies = new List<string>()
			};

			foreach (string dep in dependancies)
			{
				_state.dependancies.Add(dep);
			}

			if (RequiresPlayer)
			{
				_state.coplayer                  = Lua.RentPlayer();
				_state.coplayer.transform.parent = parent.transform;
			}

			await GameController.TillIntialized();
			Init();
			//Lua.OnReady(Init);
		}

		public void Init()
		{
			if (RequiresPlayer)
			{
				_state.coplayer.RestoreCleared();
				_state.coplayer.sourceObject = _state.parent.gameObject;
				_state.coplayer.DiscoverResources();
			}

			LuaChangeWatcher.BeginCollecting();
			_state.table = Lua.NewEnv(GetType().Name);

			_state.table["_name"]     = $"{ScriptName}";
			_state.table["container"] = this;
			_state.table["this"]      = _state.parent;

			if (RequiresPlayer)
				_state.table["coplayer"] = _state.coplayer;

			onInit?.Invoke(this);

			for (int i = 0; i < _state.dependancies.Count; i++)
			{
				Lua.envScript.DoFileSafe(_state.dependancies[i], _state.table);
			}

			LoadScript(false);

			//mono_update = _state.ScriptTable.TryGet<Closure>("update");
			//on_reset    = _state.ScriptTable.TryGet<Closure>("on_reset");

			// Keep the configuration options we set right of the bat in global
			if (RequiresPlayer)
				_state.coplayer.baseState = _state.coplayer.state;

			LuaChangeWatcher.EndCollecting(this, OnScriptChange);
		}

		protected void OnScriptChange() => _scheduledReload = true;

		public void OnUpdate(bool callLuaUpdate = true)
		{
			if (callLuaUpdate)
			{
				TryCall("update");
			}

			if (_scheduledReload)
			{
				_scheduledReload = false;
#if UNITY_EDITOR
				OnReload();
#endif
			}
		}


		public DynValue TryCall(string name, params object[] args)
		{
			if (_state.table != null && _state.table.TryGet(name, out Closure val))
			{
				return val.Call(args);
			}

			return DynValue.Nil;
		}

		public async UniTask TryPlayAsync(string name, params object[] args)
		{
			if (_state.table == null || _state.coplayer == null) return;

			CoroutineInstance instance = Lua.CreateCoroutine(_state.table, name, args, true);
			if (instance != null)
			{
				await _state.coplayer.Play(_state.table, instance);
				await UniTask.WaitUntil(() => instance == null || instance.Ended);
			}
		}

		public virtual void OnReload()
		{
			TryCall("on_reset");
			Init();
		}

		public struct RuntimeState
		{
			public Table        table;
			public Component    parent;
			public List<string> dependancies;
			public Coplayer     coplayer;
		}
	}

	public class LuaContainer : LuaContainerBase {




		public             string Name = "";
		protected override string ScriptName                     => Name;
		protected override void   LoadScript(bool editor_reload) {}
	}

	[LuaUserdata]
	public class LuaScriptContainer : LuaContainerBase, ILuaObject
	{
		public LuaScriptContainer() : base(false) { }
		public LuaScriptContainer(bool requiresPlayer) : base(requiresPlayer) { }

		[Required, PropertyOrder(-1)]
		public LuaAsset Script;
		LuaAsset ILuaObject.Script => Script;

		[HideInInspector, NonSerialized, OdinSerialize]
		public ScriptStore ScriptStore = new ScriptStore();

		protected override string ScriptName => Script ? Script.name : "no script";

		protected override void LoadScript(bool editor_reload)
		{
			if (Script == null) return;

			ScriptStore.WriteToTable(_state.table);
			Lua.LoadAssetInto(Script, _state.table);
		}

		public LuaAsset GetScript(bool optional = false) => Script;

		public ScriptStore LuaStore
		{
			get => ScriptStore;
			set => ScriptStore = value;
		}

		public string[] Requires => null;
	}
}