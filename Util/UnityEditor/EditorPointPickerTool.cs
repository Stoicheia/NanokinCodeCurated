#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public class EditorPointPickerTool
{
	public  bool            IsRunning { get; private set; }
	private Action<Vector3> onDown, onMoved, onPicked;
	private Action          onDisabled;
	public  Vector3         offset;

	public EditorPointPickerTool(Action<Vector3> onDown = null, Action<Vector3> onMoved = null, Action<Vector3> onPicked = null, Action onDisabled = null)
	{
		this.onDown     = onDown;
		this.onMoved    = onMoved;
		this.onPicked   = onPicked;
		this.onDisabled = onDisabled;
	}

	public void Enable(Action<Vector3> onDown = null, Action<Vector3> onMoved = null, Action<Vector3> onPicked = null, Action onDisabled = null)
	{
		IsRunning       = true;
		this.onDown     = onDown     ?? this.onDown;
		this.onMoved    = onMoved    ?? this.onMoved;
		this.onPicked   = onPicked   ?? this.onPicked;
		this.onDisabled = onDisabled ?? this.onDisabled;
	}

	public void EnableSimple(Action<Vector3> pickAction = null, Action onDisabled = null)
	{
		Enable(pickAction, pickAction, pickAction, onDisabled);
	}

	public void Disable()
	{
		IsRunning = false;
		onDisabled?.Invoke();
	}

	public void OnSceneGUI()
	{
		if (!IsRunning)
			return;

		Event     cur  = Event.current;
		EventType type = cur.type;

		if (type == EventType.MouseDown || type == EventType.MouseDrag || type == EventType.MouseUp)
		{
			if (cur.button == 0)
			{
				cur.Use();
			}

			if (cur.button != 0)
			{
				Disable();
				return;
			}
		}

		switch (type)
		{
			case EventType.Layout:
				// I have no idea what the hell this means but it's WORKING!!!
				int controlId = GUIUtility.GetControlID(GetHashCode(), FocusType.Passive);
				HandleUtility.AddDefaultControl(controlId);

				break;

			case EventType.MouseDown:
				onDown?.Invoke(Query());
				break;

			case EventType.MouseDrag:
				onMoved?.Invoke(Query());
				break;

			case EventType.MouseUp:
				onPicked?.Invoke(Query());
				Disable();
				break;
		}
	}

	public Vector3 Query()
	{
		// TODO maybe abstract this away a bit more so that we have a more general use case where we might not want necessarily a raycast into the map's sceneary
		Ray        ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
		RaycastHit rh;
		if (Physics.Raycast(ray, out rh, 500, Layers.Default.mask | Layers.Walkable))
			return rh.point + offset;

		return Vector3.zero; // uh idk, maybe hold and return latest good point instead?
	}
}
#endif