using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class LevelRoom : SerializedMonoBehaviour
{

	public int EntryPriority;
	public List<LevelRoomArea> roomAreas;

	public bool HasCameras => CamSettings != null;

	[HideInInspector]
	public LevelRoomCameras CamSettings;

	public void OnLevelLoad()
	{
		roomAreas = GetComponentsInChildren<LevelRoomArea>().ToList();
		CamSettings = GetComponent<LevelRoomCameras>();

		for (int i = 0; i < roomAreas.Count; i++)
		{
			roomAreas[i].room = this;
		}
		OnLevelLoadEvent.Invoke();
	}

	public void OnPlayerEnter()
	{
		OnPlayerEnterEvent.Invoke();
	}

	public void OnPlayerLeave()
	{
		OnPlayerLeaveEvent.Invoke();
	}


	public UnityEvent OnLevelLoadEvent;
	public UnityEvent OnPlayerEnterEvent;
	public UnityEvent OnPlayerLeaveEvent;
}
