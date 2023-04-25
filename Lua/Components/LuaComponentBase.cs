using System;
using System.Collections.Generic;
using System.Linq;
using Anjin.Nanokin;
using Combat.Scripting;
using MoonSharp.Interpreter;
using Overworld.Cutscenes;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;

namespace Anjin.Scripting
{
	/// <summary>
	/// - Maintain a Lua script inside a table.
	/// - Call things inside that table.
	/// - Optionally rent a player.
	/// - Addons
	/// </summary>
	public abstract class LuaComponentBase : SerializedMonoBehaviour
	{
		public Dictionary<string, UnityEngine.Object> OutsideRefs;

		protected Coplayer _player;

		[ShowInPlay, NonSerialized] public Table           ScriptTable;
		[ShowInPlay, NonSerialized] public Table           AddonTable;
		[ShowInPlay, NonSerialized] public Table           OutsideRefsTable;
		[ShowInPlay, NonSerialized] public List<ILuaAddon> Addons;
		[Space]
		[ShowInPlay, NonSerialized] public Closure on_reset;
		[ShowInPlay, NonSerialized] public Closure mono_update;

		[NonSerialized, HideInEditorMode] public bool Initialized;

		protected abstract 	string 	ScriptName { get; }
		protected abstract 	Table 	LoadScript(bool editor_reload);
		protected virtual 	void 	OnInit() { }

		bool _scheduledReload = false;

		public virtual void Start()
		{
			_player                      = Lua.RentPlayer();
			_player.transform.parent     = transform;

			Addons = GetComponentsInChildren<ILuaAddon>().ToList();

			Lua.OnReady(Init);
		}

		public virtual void Init()
		{
			_player.RestoreCleared();
			_player.sourceObject = gameObject;
			_player.DiscoverResources();
			LuaChangeWatcher.BeginCollecting();
			ScriptTable = Lua.NewEnv(GetType().Name);

			OutsideRefsTable = Lua.NewTable("OutsideRefsTable");
			AddonTable       = Lua.NewTable("AddonTable");

			//ScriptStore.WriteToTable(ScriptTable);
			ScriptTable["_name"]    = $"{ScriptName}";
			ScriptTable["coplayer"] = _player;
			ScriptTable["this"]   	= this;
			ScriptTable["refs"]   	= OutsideRefsTable;
			ScriptTable["addons"] 	= AddonTable;


			SetupAddons();
			SetupOutsideRefs();

			OnInit();

			LoadScript(false);

			mono_update = ScriptTable.TryGet<Closure>("update");
			on_reset    = ScriptTable.TryGet<Closure>("on_reset");

			// Keep the configuration options we set right of the bat in global
			_player.baseState = _player.state;

			LuaChangeWatcher.EndCollecting(this, OnScriptChange);
		}

		protected void OnScriptChange()
		{
			_scheduledReload = true;
		}

		private void OnDestroy()
		{
			Lua.ReturnPlayer(ref _player);
		}

		public void Update()
		{
			if (!GameController.IsWorldPaused) {
				CallUpdate();
			}

			if (_scheduledReload)
			{
				_scheduledReload = false;
				#if UNITY_EDITOR
				OnReload();
				#endif
			}
		}

		protected virtual void CallUpdate()
		{
			mono_update?.Call();
		}

		public virtual void OnReload()
		{
			on_reset?.Call();
			Init();
		}

		public virtual void SetupOutsideRefs()
		{
			OutsideRefsTable.Clear();

			if (OutsideRefs == null) return;

			foreach (var outsideRef in OutsideRefs) {
				try {
					OutsideRefsTable[outsideRef.Key] = outsideRef.Value;
				} catch (Exception e) {
					Debug.LogError($"SetupOutsideRefs for '{outsideRef.Key}' failed: " + e, this);
				}
			}
		}

		public virtual void SetupAddons()
		{
			AddonTable.Clear();

			for (int i = 0; i < Addons.Count; i++) {
				try {
					AddonTable[Addons[i].NameInTable] = Addons[i];
				} catch (Exception e) {
					Debug.LogError("Failed to register addon: " + Addons[i] + "Exception: " + e);
				}
			}
		}

		public virtual void RegisterAddon(ILuaAddon addon)
		{
			try {
				AddonTable[addon.NameInTable] = addon;
			} catch (Exception e) {
				Debug.LogError($"Failed to register addon: {addon}Exception: {e}");
			}
		}

		//	GENERIC
		//------------------------------------------------------
		public virtual void OnInteract()
		{ }
	}
}