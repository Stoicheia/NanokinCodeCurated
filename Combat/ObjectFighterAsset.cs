using System;
using Anjin.Scripting;
using Combat.Scripting;
using Data.Combat;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;

namespace Combat
{
	[LuaUserdata]
	public class ObjectFighterAsset : SerializedScriptableObject, ILuaObject
	{
		public GameObject Prefab;

		[Title("Values")]
		public Pointf Points;
		public Statf Stats;
		[FormerlySerializedAs("Efficiencies")]
		public Elementf Resistances;

		[SerializeField]
		public FighterBaseState BaseState;

		[Space]
		[Title("Script")]
		[NonSerialized]
		[OdinSerialize]
		[Inline]
		[Optional]
		[DarkBox(true)]
		public LuaScriptPackage script = new LuaScriptPackage
		{
			Asset = null,
			Store = new ScriptStore()
		};


		[CanBeNull]
		public LuaAsset Script => script.Asset;

		public ScriptStore LuaStore
		{
			get => script.Store;
			set => script.Store = value;
		}

		public string[] Requires => LuaUtil.battleRequires;
	}
}