using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(LevelRoomArea))]
public class LevelRoomAreaDrawer : Editor
{
	const float originLineSize = 0.7f;

	public override void OnInspectorGUI()
	{
		base.OnInspectorGUI();
	}

	private void OnDestroy()
	{
		Tools.hidden = false;
	}

	private void OnSceneGUI()
	{
		LevelRoomArea area = (LevelRoomArea) target;


		{
			if (area.areaType == LevelRoomArea.AreaType.Box)
			{
				//Draw box

				/*var p = area.transform.position;
				var ex = area.BoxBounds.extents;

				float size = 0.005f * SceneView.currentDrawingSceneView.size;
				Handles.lighting = true;
				Handles.CubeHandleCap(0, new Vector3(p.x, p.y+ex.y, p.z), Quaternion.identity, size, EventType.Repaint); //Top
				Handles.CubeHandleCap(0, new Vector3(p.x, p.y-ex.y, p.z), Quaternion.identity, size, EventType.Repaint); //Bottom
				Handles.CubeHandleCap(0, new Vector3(p.x+ex.x, p.y, p.z), Quaternion.identity, size, EventType.Repaint); //Left
				Handles.CubeHandleCap(0, new Vector3(p.x-ex.x, p.y, p.z), Quaternion.identity, size, EventType.Repaint); //Right
				Handles.CubeHandleCap(0, new Vector3(p.x, p.y, p.z+ex.z), Quaternion.identity, size, EventType.Repaint); //Forward
				Handles.CubeHandleCap(0, new Vector3(p.x, p.y, p.z-ex.z), Quaternion.identity, size, EventType.Repaint); //Backward
				Handles.lighting = true;*/

				if (Event.current.modifiers == EventModifiers.Control)
				{
					Tools.hidden = true;
					DrawPos(area.transform.position,originLineSize);

					area.BoxBounds.center =
						Handles.DoPositionHandle(area.transform.position + area.BoxBounds.center, Quaternion.identity) -
						area.transform.position;

					/*if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
						Event.current.Use();*/
				}
				else
				{
					Tools.hidden = false;
					DrawPos(area.transform.position + area.BoxBounds.center, originLineSize);
				}
			}
		}

		//Handles.BeginGUI();
		//Handles.EndGUI();
	}

	public void DrawPos(Vector3 pp,float size)
	{
		float   os    = size;
		Handles.color = Handles.xAxisColor;
		Handles.DrawLine(pp +(Vector3.left *os), pp + (Vector3.right *os));
		Handles.color = Handles.yAxisColor;
		Handles.DrawLine(pp +(Vector3.down *os), pp + (Vector3.up *os));
		Handles.color = Handles.zAxisColor;
		Handles.DrawLine(pp +(Vector3.back *os), pp + (Vector3.forward *os));
		Handles.color = Color.white;
	}

	public void DrawBox(Vector3 center, Vector3 size,Color outlineColor, Color fillColor)
	{
		/*Vector3[] points = new Vector3[]
		{
			new Vector3(size.x,1,1),
		};

		//Top
		Handles.DrawSolidRectangleWithOutline(
			new []
			{
				new Vector3(,size.y,),
			},
			fillColor,outlineColor);*/
	}
}
