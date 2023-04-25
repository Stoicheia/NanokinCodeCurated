using Anjin.Nanokin;
using Anjin.Util;
using TMPro;
using UnityEngine;
using Sirenix.OdinInspector;
using Util.Odin.Attributes;

namespace Anjin
{
	public class EmptyLevelPortal : SerializedMonoBehaviour
	{
		public LevelManifest 	Manifest;

		[WarpRef]
		public int 				TargetRecieverID = WarpReceiver.NULL_WARP;

		public WarpVolume 		Warp;
		public TextMeshPro 		Text;

		public void Start()
		{
			if (Manifest == null) {
				foreach (var t in transform.GetComponentsInChildren<Transform>()) {
					t.SetActive(false);
				}
			} else {
				Warp.TargetLevel 		= Manifest;
				Warp.TargetRecieverID 	= TargetRecieverID;
				Text.text 				= Manifest.DisplayName;
			} 

		}
	}
}