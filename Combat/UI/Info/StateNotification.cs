using System;
using Anjin.EditorUtility;
using UnityEngine;
using UnityEngine.Serialization;
using Util.Extensions;

namespace Combat.UI.Info
{
	/// <summary>
	/// A panel for a state notification
	/// </summary>
	public class StateNotification : MonoBehaviour
	{
		public StateIcon         Icon;
		public TextMeshProMulti Title;
		[FormerlySerializedAs("Duration")]
		public TextMeshProMulti Subtext;

		public Animation     Animator;
		public AnimationClip AnimEnter;
		public AnimationClip AnimEffect;
		public AnimationClip AnimExit;
		public AnimationClip AnimExpire;
		public AnimationClip AnimConsume;

		public float StayDuration = 2.5f;

		[NonSerialized]
		public float timer;

		private bool IsExitAnim => Animator.clip == AnimExit || Animator.clip == AnimExpire || Animator.clip == AnimConsume;

		private void Start()
		{
			timer = StayDuration;
		}

		public void Set(StateInfo bi)
		{
			Icon.Set(ref bi);
			Title.Text   = bi.title;
			Subtext.Text = bi.subtext;
		}

		public void Hide()
		{
			StateUI.Remove(this);
		}

		public void Update()
		{
			if (Animator.isPlaying)
			{
				timer = StayDuration;
				return;
			}

			if (!IsExitAnim)
			{
				// A timer before the exit animation
				timer -= Time.deltaTime;
				if (timer <= 0)
				{
					// Play the exit anim
					Animator.PlayClip(AnimExit);
					return;
				}
			}
			else
			{
				Animator.clip = null;
				Hide();
			}
		}
	}
}