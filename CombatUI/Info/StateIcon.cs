using UnityEngine;
using UnityEngine.UI;

namespace Combat.UI.Info
{
	public class StateIcon : MonoBehaviour
	{
		public Image         IMG_Icon;
		public Image         IMG_IconShadow;
		public Image         IMG_BGShadow;
		public Animation     Animator;
		public AnimationClip AnimEnter;
		public AnimationClip AnimExit;

		public void Set(ref StateInfo bi)
		{
			IMG_Icon.sprite       = bi.icon;
			IMG_IconShadow.sprite = bi.icon_mask;
			IMG_BGShadow.sprite   = bi.icon_mask;
		}
	}
}