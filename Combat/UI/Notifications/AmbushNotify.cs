using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using Util.RenderingElements.Trails;

namespace Combat.UI.Notifications
{
	public class AmbushNotify : StaticBoyUnity<AmbushNotify>
	{
		public TMP_Text  Text;
		public Animation Animator;
		public float     TrailWeight = 1;

		[SerializeField] private AudioDef SFX;

		[FormerlySerializedAs("AfterImager"), SerializeField]
		private Trail Trail;

		private static bool _wasPlaying;

		private void Start()
		{
			Text.gameObject.SetActive(false);
		}

		[Button]
		public static void Play()
		{
			_wasPlaying = true;

			Live.Text.gameObject.SetActive(true);
			Live.Animator.Rewind();
			Live.Animator.Play();
			Live.Trail.Play();

			GameSFX.PlayGlobal(Live.SFX);
		}

		private void LateUpdate()
		{
			Trail.opacityMultiplier = TrailWeight;

			if (_wasPlaying && !Animator.isPlaying)
			{
				_wasPlaying = false;
				Trail.StopProgressive();
				Text.gameObject.SetActive(false);
			}
		}
	}
}