using System;
using Combat.Scripting;
using MoonSharp.Interpreter;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Anjin.Scripting
{
	public class LuaScriptComponent : LuaComponentBase, ILuaObject
	{
		[Required, PropertyOrder(-1)]
		public LuaAsset Script;

		[ShowInInspector, NonSerialized, OdinSerialize]
		public ScriptStore ScriptStore = new ScriptStore();

		protected override string ScriptName => Script ? Script.name : "no script";

		protected override Table LoadScript(bool editor_reload)
		{
			ScriptStore.WriteToTable(ScriptTable);
			Lua.LoadAssetInto(Script, ScriptTable);
			return ScriptTable;
		}

		LuaAsset ILuaObject.Script => Script;

		public ScriptStore LuaStore
		{
			get => ScriptStore;
			set => ScriptStore = value;
		}

		public virtual string[] Requires => null;
	}
}