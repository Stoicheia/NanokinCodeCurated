using Anjin.UI;
using UnityEngine;
using UnityEngine.UI;

public class SetSpriteToNull : BaseStateMachineBehaviour
{
	public string GameObjectName = "";

	private Image _img;

	public override void OnLateUpdate()
	{
		if (_img) {
			_img.enabled = false;
		}
	}

	public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateExit(animator, stateInfo, layerIndex);

		if (_img) {
			_img.enabled = true;
		}
	}

	public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
	{
		base.OnStateEnter(animator, stateInfo, layerIndex);

		var obj = animator.transform.Find(GameObjectName);
		_img = obj.GetComponent<Image>();

	}
}
