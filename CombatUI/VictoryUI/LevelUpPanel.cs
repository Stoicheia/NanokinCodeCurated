using Anjin.EditorUtility;
using Anjin.Util;
using Assets.Nanokins;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using Util.Extensions;
using Util.Odin.Attributes;

namespace Combat.Components.VictoryScreen.Menu
{
	public class LevelUpPanel : MonoBehaviour
	{
		[Optional]
		public GameObject[] IMG_Values1;
		[HideIf("@IMG_Values1.Length > 0")]
		public TextMeshProMulti TMP_Value1;

		[Optional]
		public GameObject[] IMG_Values2;
		[HideIf("@IMG_Values2.Length > 0")]
		public TextMeshProMulti TMP_Value2;

		[Title("Limb")]
		public LimbImage LimbImage;
		public bool AnimatedLimb;

		[Title("Animation")]
		[Optional] public Animation Animator;
		[Optional] public AnimationClip EnterAnim;
		[Optional] public AnimationClip ExitAnim;

		public void Enter()
		{
			if (Animator != null)
				Animator.PlayClip(EnterAnim);
		}

		public void FastforwardEntrance()
		{
			if (Animator != null && Animator.clip == EnterAnim)
				Animator.SetToEnd(EnterAnim);
		}

		public async UniTask SetLimb([NotNull] NanokinLimbAsset limbAsset)
		{
			if (LimbImage != null)
			{
				await LimbImage.SetLimbAsync(limbAsset, AnimatedLimb);
			}
		}

		public void SetValue1(string value)
		{
			TMP_Value1.Text = value;
		}

		public void SetValue2(string value)
		{
			TMP_Value2.Text = value;
		}

		public void SetValue1(int value)
		{
			setValue(value, IMG_Values1, TMP_Value1);
		}

		public void SetValue2(int value)
		{
			setValue(value, IMG_Values2, TMP_Value2);
		}

		private static void setValue(int value, GameObject[] imgValues, TextMeshProMulti textValue)
		{
			if (imgValues != null && imgValues.Length > 0)
			{
				for (var i = 0; i < imgValues.Length; i++)
					imgValues[i].SetActive(i == value - 1);
			}
			else
			{
				textValue.Text = value.ToString();
			}
		}

#if UNITY_EDITOR
		[LabelText("SetLimb")]
		[Button]
		private void SetLimbButton([NotNull] NanokinLimbAsset asset)
		{
			SetLimb(asset).ForgetWithErrors();
		}
#endif
	}
}