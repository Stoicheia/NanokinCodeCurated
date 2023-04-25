using System;
using System.Collections.Generic;
using Cinemachine;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Anjin.Cameras
{
	public class CameraShot : MonoBehaviour
	{
		public static readonly List<CameraShot> all = new List<CameraShot>();

		[NonSerialized]
		public CinemachineVirtualCamera vcam;

		private void OnEnable()  => all.Add(this);
		private void OnDisable() => all.Remove(this);

		private void Awake()
		{
			vcam          = gameObject.AddComponent<CinemachineVirtualCamera>();
			vcam.Priority = GameCams.PRIORITY_INACTIVE;
		}

		private void OnDestroy()
		{
			Destroy(vcam);
		}

#if UNITY_EDITOR
		[Button]
		private void Preview()
		{
			var sceneview = SceneView.lastActiveSceneView;
			if (sceneview == null) return;

			sceneview.AlignViewToObject(transform);
		}

		[Button]
		private void Set()
		{
			var sceneview = SceneView.lastActiveSceneView;
			if (sceneview == null) return;

			transform.position   = sceneview.camera.transform.position;
			transform.rotation   = sceneview.camera.transform.rotation;
			transform.localScale = Vector3.one;
		}
#endif
	}
}