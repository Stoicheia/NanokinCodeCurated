using Anjin.Util;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


public partial class Config
{
	// public static BattleConfig  Battle  => "Battle".FetchAsset<BattleConfig>();
	public static DisplayConfig Display => "Display".FetchResource<DisplayConfig>();

#if UNITY_EDITOR

	// Menu Items
	[MenuItem("Anjin/Files/Configuration/Display")]
	public static void MI_Display() => MenuItem(Display);

	private static void MenuItem(ScriptableObject bso) => Selection.activeObject = bso;


#endif
}