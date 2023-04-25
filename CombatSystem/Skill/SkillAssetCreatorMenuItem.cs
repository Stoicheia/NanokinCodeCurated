#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using JetBrains.Annotations;
using Sirenix.Utilities;
using UnityEditor;
using UnityEngine;

namespace Combat
{
	/// <summary>
	/// Provides a right-click context menu item
	/// to automatically create a bunch of skills
	/// from the lua scripts.
	/// </summary>
	public static class SkillAssetCreatorMenuItem
	{
		private const string GameObjectMenuItemName = "GameObject/Convert/Lua to Skills";
		private const string AssetsMenuItemName     = "Assets/Convert/Lua to Skills";

		/// <summary>
		/// OnContextItem is called either:
		/// * when the user selects the menu item via the top menu (with a null MenuCommand), or
		/// * when the user selects the menu item via the context menu (in which case there's a context)
		/// OnContextItem gets called once per selected object (if the
		/// parent and child are selected, then OnContextItem will only be
		/// called on the parent)
		/// </summary>
		[MenuItem(GameObjectMenuItemName, false, 30)]
		private static void OnLuaAssetContextItem(MenuCommand command)
		{
			OnContextItem(command, SelectionMode.Editable | SelectionMode.TopLevel);
		}

		private static void OnContextItem([CanBeNull] MenuCommand command, SelectionMode mode)
		{
			LuaAsset[] selection = null;

			if (command == null || command.context == null)
			{
				// We were actually invoked from the top GameObject menu, so use the selection.
				selection = Selection.GetFiltered<LuaAsset>(SelectionMode.Unfiltered);
			}
			else
			{
				// We were invoked from the right-click menu, so use the context of the context menu.
				var selected = command.context as LuaAsset;
				if (selected)
				{
					selection = new[] { selected };
				}
			}

			if (selection == null || selection.Length == 0)
			{
				// ModelExporter.DisplayNoSelectionDialog();
				return;
			}

			Selection.objects = CreateSkills(selection).ToArray<Object>();
		}

		/// <summary>
		/// Create instantiated skill assets from a selection of luaassets.
		/// </summary>
		[SecurityPermission(SecurityAction.LinkDemand), NotNull]
		public static List<SkillAsset> CreateSkills([NotNull] LuaAsset[] assets)
		{
			List<SkillAsset> ret = new List<SkillAsset>();

			foreach (LuaAsset lua in assets)
			{
				SkillAsset skill = ScriptableObject.CreateInstance<SkillAsset>();
				skill.DisplayName      = lua.name.Replace("-", " ").SplitPascalCase();
				skill.Description      = "";
				skill.luaPackage.Asset = lua;

				string path = AssetDatabase.GetAssetPath(lua);
				string dir  = Path.GetDirectoryName(path);

				if (dir != null)
				{
					AssetDatabase.CreateAsset(skill, Path.Combine(dir, $"{lua.name}.asset"));
				}
			}

			return ret;
		}

		/// <summary>
		// Validate the menu items defined above.
		/// </summary>
		[MenuItem(GameObjectMenuItemName, true, 30)]
		[MenuItem(AssetsMenuItemName, true, 30)]
		[UsedImplicitly]
		public static bool OnValidateMenuItem() => true;

		internal static void DisplayInvalidSelectionDialog([NotNull] LuaAsset toConvert, string message = "")
		{
			EditorUtility.DisplayDialog(
				"FBX Exporter Warning",
				$"Failed to Convert: {toConvert.name}\n{message}",
				"Ok");
		}
	}
}
#endif