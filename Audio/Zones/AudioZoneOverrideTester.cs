using Sirenix.OdinInspector;
using UnityEngine;

namespace Anjin.Audio
{
	public class AudioZoneOverrideTester : SerializedMonoBehaviour
	{
		[SerializeField] private AudioClip Clip;
		[SerializeField] private string    Name;

		private AudioZone _zone;

		private void Awake()
		{
			_zone = new AudioZone
			{
				Priority  = 50,
				LerpSpeed = 0.025f,
				OverrideTrack = new AudioTrack
				{
					Clip = Clip,
					Name = Name,
					Config =
					{
						InitialVolume = 0
					}
				}
			};
		}

		private void OnEnable()
		{
			AudioManager.AddZone(_zone);
		}

		private void OnDisable()
		{
			AudioManager.RemoveZone(_zone);
		}
	}
}