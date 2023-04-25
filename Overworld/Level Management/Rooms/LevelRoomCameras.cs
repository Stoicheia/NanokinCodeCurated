using System.Collections.Generic;
using System.Linq;
using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;

[RequireComponent(typeof(LevelRoom))]
public class LevelRoomCameras : SerializedMonoBehaviour
{
	[TitleGroup("Settings")]
	public bool UseOwnCameras = true;
	[TitleGroup("Settings")]
	public bool UseCustomConfig = false;

	[TitleGroup("Settings")]
	public bool SpawnVariantsAutomatically;

	[TitleGroup("Settings")]
	[ShowIf("UseCustomConfig")]
	//public CameraConfig_OLD ConfigOld;

	[HideInEditorMode, TitleGroup("Runtime")]
	public List<CinemachineVirtualCameraBase> RoomCameras;


	private void Start()
	{
		GetComponent<LevelRoom>().OnLevelLoadEvent.AddListener(OnLevelLoad);
	}

	public void OnLevelLoad()
	{
		RoomCameras = GetComponentsInChildren<CinemachineVirtualCameraBase>().ToList();
	}
}
