using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Util.Extensions
{
	public partial class Extensions
	{
		public static void PlayClip(this UnityEngine.Animation animator, AnimationClip clip)
		{
			if (clip == null)
				return;

			animator.clip = clip;
			animator.Play();
		}

		public static void Crossfade(this UnityEngine.Animation animator, AnimationClip clip, float duration)
		{
			if (clip == null)
			{
				Debug.LogWarning("The AnimationClip to play is null! Skipping...", animator);
				return;
			}

			animator.clip = clip;
			animator.CrossFade(clip.name, duration);
		}


		public static async UniTask PlayAsync(this UnityEngine.Animation animator, AnimationClip clip)
		{
			PlayClip(animator, clip);
			await UniTask.WaitUntil(() => !animator.isPlaying);
		}

		public static void SetToEnd(this UnityEngine.Animation animation, AnimationClip clip)
		{
			if (clip == null)
			{
				Debug.LogWarning("The AnimationClip to apply the end frame is null! Skipping...", animation);
				return;
			}

			animation.clip = clip;
			animation.Play();

			foreach (AnimationState state in animation)
			{
				state.normalizedTime = 1;
			}

			animation.Sample();
			animation.Stop();
		}
	}
}