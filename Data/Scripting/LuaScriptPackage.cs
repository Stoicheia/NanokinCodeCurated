using System;
using System.Collections.Generic;
using Anjin.Scripting;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Odin.Attributes;
using Util.Odin.Selectors.File;
using Object = UnityEngine.Object;

namespace Combat.Scripting
{
	[DarkBox(true)]
	[Serializable]
	public struct LuaScriptPackage : ILuaObject
	{
		public string[] Requires => LuaUtil.battleRequires;

		[SerializeField]
		[Required]
		[CanBeNull]
		[CreateNew(memberSelectorEntries: "LuaTemplates")]
		public LuaAsset Asset;

		[SerializeField, HideInInspector]
		[CanBeNull]
		public ScriptStore Store;

#if UNITY_EDITOR
		[UsedImplicitly]
		private List<FileCreationSelectorEntry> LuaTemplates => new List<FileCreationSelectorEntry>();

		private static System.Func<Object, string> DefaultFilename => a => $"{a.name.ToLower().Replace(' ', '-')}.lua";
#endif

		[CanBeNull]
		public LuaAsset Script => Asset;

		public ScriptStore LuaStore
		{
			get => Store;
			set => Store = value;
		}
	}
}