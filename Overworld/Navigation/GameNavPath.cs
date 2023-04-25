// using Unity.Collections;
// using UnityEngine;
//
// #if UNITY_EDITOR
// using UnityEditor;
// using Sirenix.OdinInspector.Editor;
// #endif
//
// namespace Anjin.Navigation
// {
// 	public struct GameNavPath
// 	{
// 		public NativeList<Vector3> Corners;
//
// 		public GameNavPath(Vector3[] corners)
// 		{
// 			Corners = new NativeList<Vector3>(Allocator.Persistent);
//
// 			for (int i = 0; i < corners.Length; i++)
// 			{
// 				Corners.Add(corners[i]);
// 			}
// 		}
// 	}
//
// #if UNITY_EDITOR
//
// 	public class GameNavPathDrawer : OdinValueDrawer<GameNavPath>
// 	{
// 		protected override void DrawPropertyLayout(GUIContent label)
// 		{
// 			var val = ValueEntry.SmartValue;
//
// 			for (int i = 0; i < val.Corners.Length; i++)
// 			{
// 				val.Corners[i] = EditorGUILayout.Vector3Field(GUIContent.none, val.Corners[i]);
// 			}
// 		}
// 	}
//
// #endif
//
//
// }