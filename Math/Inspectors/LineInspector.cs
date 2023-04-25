#if UNITY_EDITOR
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;
using Util.Math.Splines;

namespace Assets.Scripts.Utils.Splines.Inspectors
{
	[CustomEditor(typeof(LineShape), true)]
	public class LineInspector : OdinEditor
	{
		public override void OnInspectorGUI()
		{
			DrawTree();
		}

		private void OnSceneGUI()
		{
			LineShape       line            = target as LineShape;
			Transform  handleTransform = line.transform;
			Quaternion handleRotation  = Tools.pivotRotation == PivotRotation.Local ? handleTransform.rotation : Quaternion.identity;
			Vector3    p0              = handleTransform.TransformPoint(line.LocalP1);
			Vector3    p1              = handleTransform.TransformPoint(line.LocalP2);

			Handles.color = Color.white;
			Handles.DrawLine(p0, p1);
			EditorGUI.BeginChangeCheck();
			p0 = Handles.DoPositionHandle(p0, handleRotation);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(line, "Move Point");
				EditorUtility.SetDirty(line);
				line.LocalP1 = handleTransform.InverseTransformPoint(p0);
			}

			EditorGUI.BeginChangeCheck();
			p1 = Handles.DoPositionHandle(p1, handleRotation);
			if (EditorGUI.EndChangeCheck())
			{
				Undo.RecordObject(line, "Move Point");
				EditorUtility.SetDirty(line);
				line.LocalP2 = handleTransform.InverseTransformPoint(p1);
			}
		}
	}
}
#endif