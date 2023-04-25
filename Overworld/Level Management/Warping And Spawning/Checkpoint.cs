using System;
using Anjin.Util;
using Sirenix.OdinInspector;
using UnityEngine;
using Util;
using Util.Odin.Attributes;

namespace Anjin.Nanokin {
	public class Checkpoint : MonoBehaviour {

		[Required]
		public SpawnPoint SpawnPoint;

		public Collider BoundingCollider;

		#if UNITY_EDITOR
		[Button(), ShowIf("@SpawnPoint == null")]
		public void AddSpawnPoint()
		{
			SpawnPoint                         = new GameObject("Spawn Point").ParentTo(this).AddComponent<SpawnPoint>();
			SpawnPoint.transform.localPosition = Vector3.zero;
			SpawnPoint.HideInTeleportMenu      = true;
			SpawnPoint.AddToActive             = false;
		}
		#endif

		public int Priority = -1;
	}
}