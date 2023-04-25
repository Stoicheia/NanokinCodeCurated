#if UNITY_EDITOR
using System;
using Anjin.EditorUtility;
using MeshBrush;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;
using Util.Odin.Attributes;
using EditorIcons = Sirenix.Utilities.Editor.EditorIcons;

namespace Util.UnityEditor.Launch
{
	public class NanokinLauncherWindow : OdinEditorWindow
	{
		[SerializeField, HideInInspector]
		private NanokinLauncher _launcher;

		private PropertyTree _tree;

		protected override void OnGUI()
		{
			// base.OnGUI();

			_launcher = _launcher ? _launcher : NanokinLauncher.Instance;
			_tree     = _tree ?? PropertyTree.Create(_launcher);

			_tree.Draw();
		}

		[MenuItem("Nanokin/Launcher Window")]
		public static void Show()
		{
			NanokinLauncherWindow wnd = CreateWindow<NanokinLauncherWindow>();
			wnd.name         = "Game Launch";
			wnd.titleContent = new GUIContent("Game Launch", EditorIcons.Maximize.Active);
		}
	}
}
#endif