using UnityEngine;
using UnityEngine.Animations;

namespace Anjin.UI {
	public class DisableOtherLayers : StateMachineBehaviour {

		public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
		{
			Debug.Log("On Enter");
			controller.SetBool("Disable", true);
		}

		public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex, AnimatorControllerPlayable controller)
		{
			Debug.Log("On Exit");
			controller.SetBool("Disable", false);
		}
	}
}