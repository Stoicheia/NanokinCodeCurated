using System;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Odin.Attributes;

namespace Anjin.Audio
{
	public class AudioZoneComponent : SerializedMonoBehaviour
	{
		[FormerlySerializedAs("zone"),Inline]
		public AudioZone Zone = new AudioZone();

		void OnDrawGizmos()
		{
			/*if(!zone.Global)
			{
				Gizmos.color = new Color(1, 0, 0, 0.5f);
				Gizmos.DrawWireSphere( transform.position, zone.Range);
			}*/
		}

		void OnEnable()
		{
			AudioManager.AddZone(Zone);
		}

		void OnDisable()
		{
			AudioManager.RemoveZone(Zone);
		}

		private void Update()
		{
			//Debug.Log("sdsd");
		}
	}
}